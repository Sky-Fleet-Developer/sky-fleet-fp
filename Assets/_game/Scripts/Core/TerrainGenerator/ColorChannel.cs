using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Networking;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class ColorChannel : DeformationChannel<float[], ColorMapDeformerModule>
    {
        private static readonly Semaphore Semaphore = new Semaphore(3, 3);
        private static Utilities.AsyncThreadDelegate<float[]> _readWorker = new Utilities.AsyncThreadDelegate<float[]>(Semaphore);
        [ShowInInspector, ReadOnly] public Chunk Chunk { get; private set; }
        private readonly int layersCount;
        private readonly bool normalizeAlphamap;
        private readonly string layerMaskProperty;
        private readonly RenderTexture texture;
        private readonly ComputeShader blitShader;
        public ColorChannel(Chunk chunk, ComputeShader blitShader, string layerMaskProperty, bool normalizeAlphamap, int layersCount, List<string> paths, Vector2Int position) : base(position, chunk.ChunkSize)
        {
            this.layersCount = layersCount;
            this.Chunk = chunk;
            this.blitShader = blitShader;
            this.layerMaskProperty = layerMaskProperty;
            this.normalizeAlphamap = normalizeAlphamap;
            texture = new RenderTexture(chunk.ColorMapResolution, chunk.ColorMapResolution, 0);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.enableRandomWrite = true;
            texture.Create();
            Load(paths);
        }
        

        protected override float[] GetLayerCopy(float[] source)
        {
            return source.Clone() as float[];
        }

        protected override void ApplyToCache(ColorMapDeformerModule module)
        {
            //RectangleAffectSettings rectangleSettings = GetAffectSettingsForDeformer(module.Core);

            module.WriteToChannel(this);//, rectangleSettings.minX, rectangleSettings.minY, rectangleSettings, Position, terrainData.size, layersCount);
            
           // terrainData.SetAlphamaps(rectangleSettings.minX, rectangleSettings.minY, alphamap);
        }

        protected override Task ApplyToTerrain()
        {
            if (deformationLayersCache.Count == 0) return Task.CompletedTask;

            Material material = Chunk.Material;
            Blit();
            material.SetTexture(layerMaskProperty, texture);
            return Task.CompletedTask;
        }

        private void Blit()
        {
            int kernelHandle = blitShader.FindKernel("BlitRGBA");
            using (ComputeBuffer buffer = new ComputeBuffer(Chunk.ColorMapResolution * Chunk.ColorMapResolution * layersCount, sizeof(float)))
            {
                buffer.SetData(GetLastLayer());
                blitShader.SetBuffer(kernelHandle, "input", buffer);
                blitShader.SetTexture(kernelHandle, "resultRGBA", texture);
                blitShader.SetInt("resolution", Chunk.ColorMapResolution);
                blitShader.SetInt("layersCount", layersCount);
                blitShader.Dispatch(kernelHandle, 
                    Mathf.CeilToInt(Chunk.ColorMapResolution / 8f + 0.5f),
                    Mathf.CeilToInt(Chunk.ColorMapResolution / 8f + 0.5f), 
                    1);
            }

            RenderTexture.active = texture;
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) => new RectangleAffectSettings(Chunk, Position, Chunk.Resolution, deformer);

        /*private float GetColorPerIndex(int layer, int x, int y, int n)
        {
            return deformationLayersCache[layer][x, y, n];
        }*/

        #region Loading
        private async void Load(List<string> paths)
        {
            int idx = 0;
            await Task.WhenAll(paths.Select(x => LoadAndApply(x, idx++)));

            //if(normalizeAlphamap) Normalize();
            
            loading.SetResult(true);
        }

        private async Task LoadAndApply(string path, int index)
        {
            if(path == null) return;
            //deformationLayersCache.Add(LoadAtPath(path));
            deformationLayersCache.Add(await _readWorker.RunAsync(() => LoadAtPath(path)));
        }
        
        private byte GetColorChannel(System.Drawing.Color value, int channelIdx)
        {
            switch (channelIdx)
            {
                case 0: return value.R;
                case 1: return value.G;
                case 2: return value.B;
                default: return value.A;
            }
        }
        
        private float[] LoadAtPath(string path) // TODO: get texture in edit-mode
        {
            const float divider = 1f / 255f;
            using (Bitmap bitmap = new Bitmap(path))
            {
                int resolution = bitmap.Width;
                float[] result = new float[resolution * resolution * layersCount];
                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        System.Drawing.Color pixel = bitmap.GetPixel(u, resolution - v - 1);
                        for (int w = 0; w < layersCount; w++)
                        {
                            result[(u + v * resolution) * layersCount + w] = GetColorChannel(pixel, w) * divider;
                        }
                    }
                }

                return result;
            }
            /*PNGReader.ReadPNG(path, cacheTexture);
            float[] result = new float[resolution * resolution * layersCount];
            Color[] pixels = cacheTexture.GetPixels();
            
            for (int u = 0; u < resolution; u++)
            {
                for (int v = 0; v < resolution; v++)
                {
                    Color pixel = pixels[u + v * resolution];
                    for (int w = 0; w < layersCount; w++)
                    {
                        result[(u + v * resolution) * layersCount + w] = pixel[w];
                    }
                }
            }

            return result;*/
        }

        private async Task<Texture2D> ApplyInBuild(string path)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.error != null)
                {
                    Debug.LogError(request.error);
                    return null;
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(request);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply();
                    return tex;
                }
            }
        }

        /*private void ApplyTex(Texture2D tex, int idx)
        {
            if (deformationLayersCache.Count == 0)
            {
                alphamapResolution = tex.width;
                deformationLayersCache.Add(tex);
            }
        }*/



        private void Normalize()
        {
            throw new NotImplementedException();
            /*if(deformationLayersCache.Count == 0) return;
            
            float[,,] colors = deformationLayersCache[0];
            int w = colors.GetLength(1);
            int h = colors.GetLength(0);
            //int max = Mathf.Min(3, layersCount);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float sum = 0;
                    for (int i = 0; i < layersCount; i++)
                    {
                        sum += colors[y, x, i];
                    }

                    if (sum > 1)
                    {
                        for (int i = 0; i < layersCount; i++)
                        {
                            colors[y, x, i] = colors[y, x, i] / sum;
                        }
                    }
                    else
                    {
                        colors[y, x, 0] = 1f - sum;
                    }
                    //if (sum == 0)
                    //{
                    //    colors[y, x, 0] = 1f;
                    //}
                    //else
                    //{
                    //    for (int i = 0; i < max; i++)
                    //    {
                    //        colors[y, x, i] /= sum;
                    //    }
                    //}
                }
            }*/
        }
        #endregion
    }
}
