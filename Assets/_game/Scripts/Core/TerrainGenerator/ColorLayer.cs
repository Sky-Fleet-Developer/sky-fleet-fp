using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector;
using UnityEngine.Networking;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class ColorLayer : TerrainLayer
    {
        [ShowInInspector, ReadOnly] private List<Texture2D> textures;
        private List<Color[]> colors;

        [ShowInInspector, ReadOnly] private TerrainData terrainData;
        private int layersCount;
        public ColorLayer(TerrainData terrainData, int layersCount, List<string> paths, Vector2Int chunk) : base(chunk, terrainData.size.x)
        {
            this.layersCount = layersCount;
            this.terrainData = terrainData;
            textures = new List<Texture2D>();
            Load(paths);
        }

        private int countToLoad;
        private int loadedCount;
        private void Load(List<string> paths)
        {
            List<Task> tasks = new List<Task>();
            foreach (string path in paths)
            {
                if(path == null) continue;
                Task t = LoadAtPath(path);
                tasks.Add(t);
            }

            countToLoad = tasks.Count;
            loadedCount = 0;
            if (tasks.Count == 0)
            {
                IsReady = true;
            }
        }
        
        private async Task LoadAtPath(string path) // TODO: get texture in edit-mode
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.error != null)
                {
                    Debug.LogError(request.error);
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(request);
                    tex.Apply();
                    textures.Add(tex);
                    if (++loadedCount == countToLoad)
                    {
                        ReadPixels();
                        IsReady = true;
                    }
                }
            }
        }

        private void ReadPixels()
        {
            colors = new List<Color[]>(textures.Count);
            foreach (Texture2D tex in textures)
            {
                colors.Add(tex.GetPixels());   
            }
        }

        protected override void ApplyDeformer(IDeformer deformer)
        {
            
        }

        protected override void ApplyToTerrain()
        {
            if (loadedCount == 0) return;
            
            int size = textures[0].width;
            terrainData.alphamapResolution = size;
            float[,,] sets = new float[size, size, layersCount];

            float[] temp = new float[layersCount];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float sum = 0;
                    for (int i = 0; i < layersCount; i++)
                    {
                        float res = GetColorPerIndex(x + y * size, i);
                        temp[i] = res;
                        sum += res;
                    }

                    if (sum == 0)
                    {
                        sets[x, y, 0] = 1;
                        for (int i = 1; i < layersCount; i++)
                        {
                            sets[x, y, i] = 0;
                        }
                    }
                    else
                    {
                        sum = 1 / sum;

                        for (int i = 0; i < layersCount; i++)
                        {
                            sets[x, y, i] = temp[i] * sum;
                        }
                    }
                }
            }
            terrainData.SetAlphamaps(0, 0, sets);
        }

        private float GetColorPerIndex(int n, int i)
        {
            int a = i / 3;
            int b = i % 3;
            return colors[a][n][b];
        }
    }
}