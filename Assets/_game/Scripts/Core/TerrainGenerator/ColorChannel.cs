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
    public class ColorChannel : DeformationChannel
    {
        private List<Color[]> colors;

        [ShowInInspector, ReadOnly] public TerrainData terrainData { get; private set; }
        private int layersCount;
        public ColorChannel(TerrainData terrainData, int layersCount, List<string> paths, Vector2Int chunk) : base(chunk, terrainData.size.x)
        {
            this.layersCount = layersCount;
            this.terrainData = terrainData;
            Load(paths);
        }

        private int alphamapResolution;
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
            if (colors == null)
            {
                colors = new List<Color[]>();
                alphamapResolution = tex.width;
            }
            colors.Add(tex.GetPixels());
        }

        protected override void ApplyDeformer(IDeformer deformer)
        {
            ColorMapDeformerModule deformerModule = deformer.GetModules<ColorMapDeformerModule>();
            if (deformerModule == null) return;

            RectangleAffectSettings rectangleSettings = GetAffectSettingsForDeformer(deformer);

            float[,,] alphamap = terrainData.GetAlphamaps(rectangleSettings.minX, rectangleSettings.minY,
                rectangleSettings.deltaX, rectangleSettings.deltaY);
            
            deformerModule.WriteToAlphamaps(alphamap, 0, 0, rectangleSettings, Position, terrainData.size, layersCount);
            
            terrainData.SetAlphamaps(rectangleSettings.minX, rectangleSettings.minY, alphamap);
        }

        protected override void ApplyToTerrain()
        {
            if (loadedCount == 0) return;
            
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
                        float res = GetColorPerIndex(x + y * alphamapResolution, i);
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

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) => new RectangleAffectSettings(terrainData, Position, terrainData.alphamapResolution, deformer);

        private float GetColorPerIndex(int n, int i)
        {
            int a = i / 3;
            int b = i % 3;
            return colors[a][n][b];
        }
    }
}
