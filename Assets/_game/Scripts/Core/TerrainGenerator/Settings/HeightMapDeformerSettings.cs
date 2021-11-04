using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class HeightMapDeformerSettings : IDeformerLayerSetting
    {
        public float[] Heights;
        public Vector2Int Resolution;
        public Dictionary<TerrainLayer, Dictionary<Vector2Int, HeightCache>> cache;
        
        [JsonIgnore] public Deformer Core { get; set; }
        public void Init(Deformer core)
        {
            Core = core;
        }
        
        [Button]
        public void ReadFromTerrain()
        {
            Heights = new float[Resolution.x * Resolution.y];
            for(int i = 0; i < Resolution.x; i++)
            {
                for (int i2 = 0; i2 < Resolution.y; i2++)
                {
                    Vector3 pos = Core.transform.rotation * new Vector3((i / (Resolution.x - 1f) - 0.5f) * Core.LocalRect.z, 6000, (i2 / (Resolution.y - 1f) - 0.5f) * Core.LocalRect.w);
                    pos += Core.transform.position;
                    Ray ray = new Ray(pos, -Vector3.up);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                    {
                        float h = hit.point.y - Core.transform.position.y;
                        Heights[i * Resolution.y + i2] = h;
                        Debug.DrawLine(hit.point, hit.point + Vector3.down * h, h < 0 ? Color.green : Color.red, 5);
                    }
                }
            }
        }

        [Button]
        public void WriteToTerrain()
        {
            WriteToTerrain(Core.GetTerrainsContacts());
        }

        public void WriteToTerrain(Terrain[] terrains)
        {
            foreach (Terrain terrain in terrains)
            {
                var settings = new RectangleAffectSettings(terrain, Core);

                float[,] heights = terrain.terrainData.GetHeights(settings.minX, settings.minY, settings.deltaX, settings.deltaY);

                Dictionary<Vector2Int, HeightCache> map = CalculateMap(settings, terrain.transform.position, terrain.terrainData.size);
                
                WriteToHeightmap(map, heights, 0, 0, settings);
                
#if UNITY_EDITOR
Undo.RecordObject(terrain.terrainData, "change terrain");
#endif
                terrain.terrainData.SetHeights(settings.minX, settings.minY, heights);
                
#if UNITY_EDITOR
EditorUtility.SetDirty(terrain.terrainData);
#endif
            }
        }

        public void CalculateCache(TerrainLayer layer, RectangleAffectSettings settings, Vector3 terrainPosition, Vector3 terrainSize)
        {
            if (cache == null) cache = new Dictionary<TerrainLayer, Dictionary<Vector2Int, HeightCache>>();
            cache.Add(layer, CalculateMap(settings, terrainPosition, terrainSize));
        }

        private Dictionary<Vector2Int, HeightCache> CalculateMap(RectangleAffectSettings settings, Vector3 terrainPosition, Vector3 terrainSize)
        {
            float ceilSize = terrainSize.x / settings.resolution;
            float heightInv = 1f / terrainSize.y;
            float hMid = Core.transform.position.y;

            Dictionary<Vector2Int, HeightCache> map = new  Dictionary<Vector2Int, HeightCache>();
                
            for (int x = 0; x < settings.deltaX; x++)
            {
                for (int y = 0; y < settings.deltaY; y++)
                {
                    Vector3 worldPos = terrainPosition + new Vector3((settings.minX + x) * ceilSize, 0, (settings.minY + y) * ceilSize);

                    Vector2 localCoordinates = GetLocalPointCoordinates(worldPos);
                        
                    if(localCoordinates.x < 0 || localCoordinates.x > 1 || localCoordinates.y < 0 || localCoordinates.y > 1) continue;

                    float hAdd = (GetHeightBilinear(localCoordinates.x, localCoordinates.y) + hMid) * heightInv;

                    map.Add(new Vector2Int(x, y), new HeightCache(hAdd, GetOpacity(localCoordinates.x, localCoordinates.y)));
                }
            }

            return map;
        }

        public void WriteToHeightmap(Dictionary<Vector2Int, HeightCache> map, float[,] heights, int xBegin, int yBegin, RectangleAffectSettings settings)
        {
            for (int x = 0; x < settings.deltaX; x++)
            {
                for (int y = 0; y < settings.deltaY; y++)
                {
                    if (!map.TryGetValue(new Vector2Int(x, y), out HeightCache m)) continue;
                    
                    float hDelta = m.height - heights[yBegin + y, xBegin + x];
                    
                    heights[yBegin + y, xBegin + x] += hDelta * m.alpha;
                }
            }
        }

        private Vector2 GetLocalPointCoordinates(Vector3 worldPos)
        {
            Vector3 localPos = Core.transform.InverseTransformPoint(worldPos);

            float x = localPos.x;
            float y = localPos.z;

            Vector4 lRect = Core.LocalRect;

            x = (x - lRect.x) + lRect.z * 0.5f;
            y = (y - lRect.y) + lRect.w * 0.5f;

            x /= lRect.z;
            y /= lRect.w;

            return new Vector2(x, y);
        }

        private float GetOpacity(float x, float y)
        {
            float fade = Core.Fade;
            float xOpacity = Mathf.Min((1f - Mathf.Abs(x - 0.5f) * 2f) / fade, 1f);
            float yOpacity = Mathf.Min((1f - Mathf.Abs(y - 0.5f) * 2f) / fade, 1f);
            return Mathf.Min(xOpacity, yOpacity);
        }

        private float GetHeightBilinear(float x, float y)
        {
            float xPos = x * Resolution.x;
            float yPos = y * Resolution.y;
            
            int minX = Mathf.Min(Mathf.FloorToInt(xPos), Resolution.x - 2);
            int minY = Mathf.Min(Mathf.FloorToInt(yPos), Resolution.y - 2);

            float remainsX = xPos - minX;
            float remainsY = yPos - minY;

            float topValue = Mathf.LerpUnclamped(Heights[minX * Resolution.y + minY],
                Heights[minX * Resolution.y + Resolution.y + minY], remainsX);
            float bottomValue = Mathf.LerpUnclamped(Heights[minX * Resolution.y + minY + 1],
                Heights[minX * Resolution.y + Resolution.y + minY + 1], remainsX);

            return Mathf.LerpUnclamped(topValue, bottomValue, remainsY);
        }

    }
    
    public struct HeightCache
    {
        public float height;
        public float alpha;

        public HeightCache(float height, float alpha)
        {
            this.height = height;
            this.alpha = alpha;
        }
    }

    public class RectangleAffectSettings
    {
        public int resolution;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public int deltaX;
        public int deltaY;

        public RectangleAffectSettings(Terrain terrain, IDeformer deformer)
        {
            resolution = terrain.terrainData.heightmapResolution;
            Rect rect = Deformer.GetAffectRectangle(terrain, deformer.AxisAlignedRect);
            minX = Mathf.CeilToInt(rect.x * resolution);
            minY = Mathf.CeilToInt(rect.y * resolution);
            minX = Mathf.Max(minX, 0);
            minY = Mathf.Max(minY, 0);
            maxX = Mathf.FloorToInt(rect.xMax * resolution);
            maxY = Mathf.FloorToInt(rect.yMax * resolution);
            maxX = Mathf.Min(maxX, resolution);
            maxY = Mathf.Min(maxY, resolution);
            deltaX = maxX - minX;
            deltaY = maxY - minY;
        }
        public RectangleAffectSettings(TerrainData data, Vector3 terrainPosition, int resolution, IDeformer deformer)
        {
            this.resolution = resolution;
            Rect rect = Deformer.GetAffectRectangle(data, terrainPosition, deformer.AxisAlignedRect);
            minX = Mathf.CeilToInt(rect.x * resolution);
            minY = Mathf.CeilToInt(rect.y * resolution);
            minX = Mathf.Max(minX, 0);
            minY = Mathf.Max(minY, 0);
            maxX = Mathf.FloorToInt(rect.xMax * resolution);
            maxY = Mathf.FloorToInt(rect.yMax * resolution);
            maxX = Mathf.Min(maxX, resolution);
            maxY = Mathf.Min(maxY, resolution);
            deltaX = maxX - minX;
            deltaY = maxY - minY;
        }
    }
}