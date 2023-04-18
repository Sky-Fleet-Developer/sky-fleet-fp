using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Networking;
using Color = UnityEngine.Color;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class ColorChannel : DeformationChannel<float[,,], ColorMapDeformerModule>
    {
        [ShowInInspector, ReadOnly] public TerrainData terrainData { get; private set; }
        private int layersCount;
        private List<IColorFilter> filters;
        private bool normalizeAlphamap;
        public ColorChannel(TerrainData terrainData, List<IColorFilter> filters, bool normalizeAlphamap, int layersCount, List<string> paths, Vector2Int chunk) : base(chunk, terrainData.size.x)
        {
            this.layersCount = layersCount;
            this.terrainData = terrainData;
            this.filters = filters;
            if (this.filters == null) this.filters = new List<IColorFilter>();
            this.normalizeAlphamap = normalizeAlphamap;
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
            if (deformationLayersCache.Count == 0) return;
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
        private async void Load(List<string> paths)
        {
            int idx = 0;
            await Task.WhenAll(paths.Select(x => LoadAndApply(x, idx++)));

            if(normalizeAlphamap) Normalize();
            
            IsReady = true;
        }

        private async Task LoadAndApply(string path, int index)
        {
            if(path == null) return;
            Texture2D texture = await LoadAtPath(path);
            await ApplyTex(texture, index);
        }
        
        private async Task<Texture2D> LoadAtPath(string path) // TODO: get texture in edit-mode
        {
/*#if UNITY_EDITOR
            Bitmap bitmap = new Bitmap(path);
            var tex = new Texture2D(bitmap.Width, bitmap.Height);
            PNGReader.ReadPNG(path, tex);
            tex.Apply();
            return tex;
#else*/
            return await ApplyInBuild(path);
//#endif
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
                    tex.Apply();
                    return tex;
                }
            }
        }

        private Task ApplyTex(Texture2D tex, int idx)
        {
            if (deformationLayersCache.Count == 0)
            {
                alphamapResolution = tex.width;
                deformationLayersCache.Add(new float[alphamapResolution, alphamapResolution, layersCount]);
            }

            return ReadPixels(tex, idx);
        }

        private async Task ReadPixels(Texture2D tex, int idx)
        {
            float[,,] colors = deformationLayersCache[0];
            Color[] pixels = tex.GetPixels();
            int w = tex.width;
            int h = tex.height;
            int maxFromZero = Mathf.Min(3, layersCount);
            int min = Mathf.Min(idx * 3, layersCount);

            for (int x = 0; x < w; x++)
            {
                await Task.Yield();
                for (int y = 0; y < h; y++)
                {
                    int i2 = min;
                    var color = pixels[x + y * alphamapResolution];
                    foreach (IColorFilter colorFilter in filters)
                    {
                        color = colorFilter.Evaluate(color);
                    }
                    
                    for (int i = 0; i < maxFromZero; i++)
                    {
                        colors[y, x, i2++] = color[i];
                    }
                }
            }
        }

        private void Normalize()
        {
            if(deformationLayersCache.Count == 0) return;
            
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
                    /*if (sum == 0)
                    {
                        colors[y, x, 0] = 1f;
                    }
                    else
                    {
                        for (int i = 0; i < max; i++)
                        {
                            colors[y, x, i] /= sum;
                        }
                    }*/
                }
            }
        }
        #endregion
    }
}
