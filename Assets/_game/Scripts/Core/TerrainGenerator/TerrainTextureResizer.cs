using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;

namespace Core.TerrainGenerator
{
    public class TerrainTextureResizer : MonoBehaviour, ILoadAtStart
    {
        [SerializeField] private string targetDirectory;
        [SerializeField] private FormatContainer[] formats;
        [SerializeField] private int compressionRatio = 2;
    
        [System.Serializable]
        private class FormatContainer
        {
            public bool enabled = true;
            public FileFormatSeeker originalFormat;
            public FileFormatSeeker resizedFormat;
            public FileFormatSeeker cutChunksFormat;
            public ComputeShader shader;
        }
    
        private abstract class Worker
        {
            public abstract void Resize(string originPath, string resizedPath, ComputeShader shader, int compressionRatio);
            public abstract void SyncHorizontal(string left, string right);
            public abstract void SyncVertical(string top, string bottom);
            public abstract void Cut(string path, string blPath, string brPath, string tlPath, string rtPath);
        }
    
        private class RawWorker : Worker
        {
            public override void Resize(string originPath, string resizedPath, ComputeShader shader, int compressionRatio)
            {
                float[,] origin = RawReader.ReadArray(originPath);
            
                int kernelHandle = shader.FindKernel("compute_shader");
            
                int width = origin.GetLength(0);
                int height = origin.GetLength(1);
                int compressedWidth = (width - 1) / compressionRatio + 1;
                int compressedHeight = (height - 1) / compressionRatio + 1;
            
                ComputeBuffer buffer = new ComputeBuffer(width * height, sizeof(float));
                ComputeBuffer outBuffer = new ComputeBuffer(compressedWidth * compressedHeight, sizeof(float));
                buffer.SetData(origin);
            
                shader.SetBuffer(kernelHandle, "input", buffer);
                shader.SetBuffer(kernelHandle, "result", outBuffer);
                shader.SetInt("inputSize", height);
                shader.SetInt("outputSize", compressedHeight);
                shader.SetInt("compressionRatio", compressionRatio);
            
                shader.Dispatch(kernelHandle, Mathf.CeilToInt(compressedWidth / 8f + 0.5f), Mathf.CeilToInt(compressedHeight / 8f + 0.5f), 1);
            
                float[,] result = new float[compressedWidth, compressedHeight];
                outBuffer.GetData(result);
            
                buffer.Dispose();
                outBuffer.Dispose();
            
                RawReader.WriteRaw16(result, resizedPath);
            }
            
            public override void SyncHorizontal(string right, string left)
            {
                float[,] r = RawReader.ReadArray(right);
                float[,] l = RawReader.ReadArray(left);

                int size = r.GetLength(0);
                int last = size - 1;
                for (int i = 0; i < size; i++)
                {
                    float mid = (r[i, 0] + l[i, last]) * 0.5f;
                    r[i, 0] = mid;
                    l[i, last] = mid;
                }
                RawReader.WriteRaw16(r, right);
                RawReader.WriteRaw16(l, left);
            }

            public override void SyncVertical(string top, string bottom)
            {
                float[,] t = RawReader.ReadArray(top);
                float[,] b = RawReader.ReadArray(bottom);

                int size = t.GetLength(1);
                int last = size - 1;
                for (int i = 0; i < size; i++)
                {
                    float mid = (t[0, i] + b[last, i]) * 0.5f;
                    t[0, i] = mid;
                    b[last, i] = mid;
                }
                RawReader.WriteRaw16(t, top);
                RawReader.WriteRaw16(b, bottom);
            }

            public override void Cut(string path, string blPath, string brPath, string tlPath, string trPath)
            {
                float[,] origin = RawReader.ReadArray(path);
                int res = origin.GetLength(0);
                int partRes = (res - 1) / 2 + 1;
                float[,] cache = new float[partRes, partRes];

                CopyFromTo(origin, cache, 0, 0);
                RawReader.WriteRaw16(cache, blPath);
                CopyFromTo(origin, cache, partRes - 1, 0);
                RawReader.WriteRaw16(cache, brPath);
                CopyFromTo(origin, cache, 0, partRes - 1);
                RawReader.WriteRaw16(cache, tlPath);
                CopyFromTo(origin, cache, partRes - 1, partRes - 1);
                RawReader.WriteRaw16(cache, trPath);
            }

            private void CopyFromTo(float[,] origin, float[,] destination, int startX, int startY)
            {
                int size = destination.GetLength(0);
                for (int u = 0; u < size; u++)
                {
                    for (int v = 0; v < size; v++)
                    {
                        destination[u, v] = origin[u + startX, v + startY];
                    }
                }
            }
        }
        
        private class PngWorker : Worker
        {
            public override void Resize(string originPath, string resizedPath, ComputeShader shader, int compressionRatio)
            {
            }

            public override void SyncHorizontal(string left, string right)
            {
            }

            public override void SyncVertical(string top, string bottom)
            {
            }

            public override void Cut(string path, string blPath, string brPath, string tlPath, string trPath)
            {
                int res = 0;
                using (Bitmap bitmap = new Bitmap(path))
                {
                    res = bitmap.Width;
                }
                Texture2D texture = new Texture2D(res, res);
                PNGReader.ReadPNG(path, texture);
                Color[] origin = texture.GetPixels();
                int partRes = (res - 1) / 2 + 1;
                
                Color[] cache = new Color[partRes * partRes];
                Texture2D dest = new Texture2D(partRes, partRes);
                CopyFromTo(origin, cache, res, partRes, 0, 0);
                dest.SetPixels(cache);
                dest.Apply();
                File.WriteAllBytes(blPath, dest.EncodeToPNG());
                
                CopyFromTo(origin, cache, res, partRes, partRes - 1, 0);
                dest.SetPixels(cache);
                dest.Apply();
                File.WriteAllBytes(brPath, dest.EncodeToPNG());
                
                CopyFromTo(origin, cache, res, partRes, 0, partRes - 1);
                dest.SetPixels(cache);
                dest.Apply();
                File.WriteAllBytes(tlPath, dest.EncodeToPNG());
                
                CopyFromTo(origin, cache, res, partRes, partRes - 1, partRes - 1);
                dest.SetPixels(cache);
                dest.Apply();
                File.WriteAllBytes(trPath, dest.EncodeToPNG());
                
                DestroyImmediate(texture);
                DestroyImmediate(dest);
            }

            private void CopyFromTo(Color[] origin, Color[] destination, int originSize, int destSize, int startX, int startY)
            {
                for (int u = 0; u < destSize; u++)
                {
                    for (int v = 0; v < destSize; v++)
                    {
                        destination[u * destSize + v] = origin[(u + startX) * originSize + v + startY];
                    }
                }
            }
        }

        private Dictionary<string, Worker> workers;

        private void Awake()
        {
            workers = new Dictionary<string, Worker>
            {
                {"r16", new RawWorker()},
                {"png", new PngWorker()}
            };
        }

        public async Task Load()
        {
            if (TryGetDirectory(out DirectoryInfo directory)) return;
            foreach (FormatContainer formatContainer in formats)
            {
                if(!formatContainer.enabled || string.IsNullOrEmpty(formatContainer.originalFormat.format)) continue;
                
                Dictionary<Vector2Int, string> paths = formatContainer.originalFormat.SearchInFolder(directory.FullName);
                Worker worker = workers[formatContainer.originalFormat.extension];
                foreach (KeyValuePair<Vector2Int, string> kv in paths)
                {
                    string resizedPath = formatContainer.resizedFormat.GetPathByFormat(kv.Key, directory.FullName);
                    if (File.Exists(resizedPath)) continue;
                    worker.Resize(kv.Value, resizedPath, formatContainer.shader, compressionRatio);
                    await Task.Yield();
                }
                
                paths = formatContainer.resizedFormat.SearchInFolder(directory.FullName);
                Sync2(paths, Vector2Int.up, worker.SyncVertical);
                Sync2(paths, Vector2Int.right, worker.SyncHorizontal);
            }
        }

        [Button]
        private void SyncOriginalNeighborsBtn()
        {
            Awake();
            SyncOriginalNeighbors();
        }

        private void SyncOriginalNeighbors()
        {
            if (TryGetDirectory(out DirectoryInfo directory)) return;
            foreach (FormatContainer formatContainer in formats)
            {
                if (!formatContainer.enabled || string.IsNullOrEmpty(formatContainer.originalFormat.format)) continue;

                Dictionary<Vector2Int, string> paths = formatContainer.originalFormat.SearchInFolder(directory.FullName);
                Worker worker = workers[formatContainer.originalFormat.extension];
                Sync2(paths, Vector2Int.up, worker.SyncVertical);
                Sync2(paths, Vector2Int.right, worker.SyncHorizontal);
            }
        }

        [Button]
        private void CutOriginalBtn()
        {
            Awake();
            CutOriginal();
        }

        private void CutOriginal()
        {
            if (TryGetDirectory(out DirectoryInfo directory)) return;
            foreach (FormatContainer formatContainer in formats)
            {
                if (!formatContainer.enabled || string.IsNullOrEmpty(formatContainer.originalFormat.format)) continue;

                string directoryName = directory.FullName;
                Dictionary<Vector2Int, string> paths = formatContainer.originalFormat.SearchInFolder(directoryName);
                Worker worker = workers[formatContainer.originalFormat.extension];
                foreach (KeyValuePair<Vector2Int, string> kv in paths)
                {
                    worker.Cut(kv.Value,
                        formatContainer.cutChunksFormat.GetPathByFormat(kv.Key * 2, directoryName),
                        formatContainer.cutChunksFormat.GetPathByFormat(kv.Key * 2 + Vector2Int.up, directoryName),
                        formatContainer.cutChunksFormat.GetPathByFormat(kv.Key * 2 + Vector2Int.right, directoryName),
                        formatContainer.cutChunksFormat.GetPathByFormat(kv.Key * 2 + Vector2Int.one, directoryName)
                    );
                }
            }
        }

        private bool TryGetDirectory(out DirectoryInfo directory)
        {
            directory = DirectoryUtilities.GetDirectory(targetDirectory);
            if (directory == null)
            {
                Debug.LogWarning("Wrong directory!");
                return true;
            }

            return false;
        }

        private static void Sync2(Dictionary<Vector2Int, string> paths, Vector2Int direction, Action<string, string> syncAction)
        {
            foreach (KeyValuePair<Vector2Int, string> kv in paths)
            {
                Vector2Int nextKey = kv.Key + direction;
                if (paths.ContainsKey(nextKey))
                {
                    syncAction(paths[nextKey], paths[kv.Key]);
                }
            }
        }


        /*private Texture2D CompressWithShader(Texture2D originalTexture)
        {
            int kernelHandle = downSamplingShader.FindKernel("compute_shader");

            int width = originalTexture.width / compressionRatio;
            int height = originalTexture.height / compressionRatio;
            RenderTexture outputTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            outputTexture.enableRandomWrite = true;
            outputTexture.Create();

            downSamplingShader.SetTexture(kernelHandle, "inputTexture", originalTexture);
            downSamplingShader.SetTexture(kernelHandle, "outputTexture", outputTexture);
            
            downSamplingShader.Dispatch(kernelHandle, width / 8, height / 8, 1);
            RenderTexture.active = outputTexture;
            Texture2D outputTexture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            outputTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            outputTexture2D.Apply();
            RenderTexture.ReleaseTemporary(outputTexture);
            return outputTexture2D;
        }
        
        private static Texture2D CompressTexture(Texture2D originalTexture, int compressionRatio)
        {
            int newWidth = originalTexture.width / compressionRatio;
            int newHeight = originalTexture.height / compressionRatio;
            Texture2D compressedTexture = new Texture2D(newWidth, newHeight);

            Color[] originalPixels = originalTexture.GetPixels();

            Color[] compressedPixels = new Color[newWidth * newHeight];

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int originalX = x * compressionRatio;
                    int originalY = y * compressionRatio;
                    int originalIndex = originalY * originalTexture.width + originalX;

                    Color averageColor = new Color(0, 0, 0, 0);
                    for (int i = 0; i < compressionRatio; i++)
                    {
                        for (int j = 0; j < compressionRatio; j++)
                        {
                            int index = originalIndex + i * originalTexture.width + j;
                            averageColor += originalPixels[index];
                        }
                    }
                    averageColor /= compressionRatio * compressionRatio;

                    int compressedIndex = y * newWidth + x;
                    compressedPixels[compressedIndex] = averageColor;
                }
            }

            compressedTexture.SetPixels(compressedPixels);
            compressedTexture.Apply();

            return compressedTexture;
        }*/
    }
}
