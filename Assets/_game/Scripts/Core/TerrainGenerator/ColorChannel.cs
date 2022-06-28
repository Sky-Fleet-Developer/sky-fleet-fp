using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector;
using UnityEngine.Networking;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class ColorChannel : DeformationChannel<float[,,], ColorMapDeformerModule>
    {

        [ShowInInspector, ReadOnly] public TerrainData terrainData { get; private set; }
        private int layersCount;
        public ColorChannel(TerrainData terrainData, int layersCount, List<string> paths, Vector2Int chunk) : base(chunk, terrainData.size.x)
        {
            this.layersCount = layersCount;
            this.terrainData = terrainData;
            Load(paths);
        }
        

        protected override float[,,] GetLayerCopy(float[,,] source)
        {
            return source.Clone() as float[,,];
        }

        protected override void ApplyToCache(ColorMapDeformerModule module)
        {
            //RectangleAffectSettings rectangleSettings = GetAffectSettingsForDeformer(module.Core);

            module.WriteToChannel(this);//, rectangleSettings.minX, rectangleSettings.minY, rectangleSettings, Position, terrainData.size, layersCount);
            
           // terrainData.SetAlphamaps(rectangleSettings.minX, rectangleSettings.minY, alphamap);
        }

        protected override void ApplyToTerrain()
        {
            if (loadedCount == 0) return;

            /*int lastLayer = deformationLayersCache.Count - 1;
            
            terrainData.alphamapResolution = alphamapResolution;
            float[,,] sets = new float[alphamapResolution, alphamapResolution, layersCount];

            float[] temp = new float[layersCount];

            for (int x = 0; x < alphamapResolution; x++)
            {
                for (int y = 0; y < alphamapResolution; y++)
                {
                    float sum = 0;
                    for (int i = 0; i < layersCount; i++)
                    {
                        float res = GetColorPerIndex(lastLayer, x, y, i);
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
            }*/
            
            terrainData.SetAlphamaps(0, 0, GetLastLayer());
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) => new RectangleAffectSettings(terrainData, Position, terrainData.alphamapResolution, deformer);

        /*private float GetColorPerIndex(int layer, int x, int y, int n)
        {
            return deformationLayersCache[layer][x, y, n];
        }*/

        #region Loading
        private int alphamapResolution;
        private int countToLoad;
        private int loadedCount;
        private async void Load(List<string> paths)
        {
            countToLoad = paths.Count;
            List<Task> tasks = new List<Task>();
            foreach (string path in paths)
            {
                if(path == null) continue;
                Task t = LoadAtPath(path);
                tasks.Add(t);
            }

            loadedCount = 0;
            if (tasks.Count == 0)
            {
                IsReady = true;
            }
            foreach (Task task in tasks)
            {
                await task;
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
                    if (deformationLayersCache.Count == 0)
                    {
                        alphamapResolution = tex.width;
                        deformationLayersCache.Add(new float[alphamapResolution, alphamapResolution, layersCount]);
                    }
                    ReadPixels(tex);
                    if (++loadedCount == countToLoad)
                    {
                        IsReady = true;
                    }
                }
            }
        }

        private void ReadPixels(Texture2D tex)
        {
            float[,,] colors = deformationLayersCache[0];
            Color[] pixels = tex.GetPixels();
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    int max = Mathf.Min(loadedCount + 4, layersCount);
                    int i2 = 0;
                    for (int i = loadedCount; i < max; i++)
                    {
                        colors[x, y, i2++] = pixels[x + y * alphamapResolution][i];
                    }
                }
            }
        }
        #endregion
    }
}
