using System.Collections.Generic;
using System.Linq;
using Core.TerrainGenerator.Utility;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class HeightMapDeformerModule : IDeformerModule
    {
        public float[] Heights;
        public Vector2Int Resolution;
        public bool alignWithTerrain = true;
        //public Dictionary<DeformationChannel, Dictionary<Vector2Int, HeightCache>> cache;
        
        [JsonIgnore] public IDeformer Core { get; set; }
        public void Init(IDeformer core)
        {
            Core = core;
        }
        
        public void ReadFromTerrain()
        {
            Heights = new float[Resolution.x * Resolution.y];
            for(int i = 0; i < Resolution.x; i++)
            {
                for (int i2 = 0; i2 < Resolution.y; i2++)
                {
                    Vector3 pos = Core.Rotation * new Vector3((i / (Resolution.x - 1f) - 0.5f) * Core.LocalRect.z + Core.LocalRect.x, 6000, (i2 / (Resolution.y - 1f) - 0.5f) * Core.LocalRect.w + Core.LocalRect.y);
                    pos += Core.Position;
                    Ray ray = new Ray(pos, -Vector3.up);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                    {
                        float h = hit.point.y - Core.Position.y;
                        Heights[i * Resolution.y + i2] = h;
                        Debug.DrawLine(hit.point, hit.point + Vector3.down * h, h < 0 ? Color.green : Color.red, 5);
                    }
                }
            }
        }

        /*[Button]
        public void WriteToChannel()
        {
            WriteToChannel(Core.GetTerrainsContacts());
        }*/

        public void WriteToChannel(DeformationChannel sourceChannel)
        {
            if (!(sourceChannel is HeightChannel channel)) return;

            RectangleAffectSettings settings = sourceChannel.GetAffectSettingsForDeformer(Core);

            float[,] source = channel.GetSourceLayer(Core);
            float[][,] destination = channel.GetDestinationLayers(Core).ToArray();

            Dictionary<Vector2Int, HeightCache>
                map = CalculateMap(settings, channel.Position, channel.chunk.ChunkSize, channel.chunk.Height);

            WriteToHeightmap(map, source, destination, settings);

/*#if UNITY_EDITOR
Undo.RecordObject(terrain.terrainData, "change terrain");
#endif*/
            //channel.terrainData.SetHeights(settings.minX, settings.minY, source);

/*#if UNITY_EDITOR
EditorUtility.SetDirty(terrain.terrainData);
#endif*/
        }

        /*public void CalculateCache(TerrainData terrainData, HeightChannel channel, RectangleAffectSettings settings, Vector3 terrainPosition, Vector3 terrainSize)
        {
            if (cache == null) cache = new float[settings.deltaX, settings.deltaY];

            float[,] prevLayerCache = channel.GetLayerCache(Core);
            
        }*/

        private Dictionary<Vector2Int, HeightCache> CalculateMap(RectangleAffectSettings rectSettings, Vector3 terrainPosition, float size, float height)
        {
            float ceilSize = size / rectSettings.resolution;
            float heightInv = 1f / height;
            float hMid = Core.Position.y;

            Dictionary<Vector2Int, HeightCache> map = new  Dictionary<Vector2Int, HeightCache>();
                
            for (int x = 0; x < rectSettings.deltaX; x++)
            {
                for (int y = 0; y < rectSettings.deltaY; y++)
                {
                    Vector3 worldPos = terrainPosition + new Vector3((rectSettings.minX + x) * ceilSize, 0, (rectSettings.minY + y) * ceilSize);

                    Vector2 localCoordinates = Core.GetLocalPointCoordinates(worldPos);
                        
                    if(localCoordinates.x < 0 || localCoordinates.x > 1 || localCoordinates.y < 0 || localCoordinates.y > 1) continue;

                    float hAdd = (GetHeightBilinear(localCoordinates.x, localCoordinates.y) + hMid) * heightInv;

                    map.Add(new Vector2Int(x, y), new HeightCache(hAdd, GetOpacity(localCoordinates.x, localCoordinates.y)));
                }
            }

            return map;
        }

        public void WriteToHeightmap(Dictionary<Vector2Int, HeightCache> map, float[,] source, float[][,] destination, RectangleAffectSettings settings)
        {
            for (int x = 0; x < settings.deltaX; x++)
            {
                for (int y = 0; y < settings.deltaY; y++)
                {
                    if (!map.TryGetValue(new Vector2Int(x, y), out HeightCache m)) continue;

                    float s = source[settings.minY + y, settings.minX + x];
                    
                    float hDelta = m.height - s;

                    float result = s + hDelta * m.alpha;
                    int a = settings.minY + y, b = settings.minX + x;
                    for (var i = 0; i < destination.Length; i++)
                    {
                        destination[i][a, b] = result;
                    }
                }
            }
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

            if (Heights == null || Heights.Length == 0)
            {
                return 0;
            }

            if (Heights.Length == 1)
            {
                return Heights[0];
            }
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
            Rect rect = MathfUtilities.GetAffectRectangle(terrain, deformer.AxisAlignedRect);
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
        public RectangleAffectSettings(Chunk chunk, Vector3 terrainPosition, int resolution, IDeformer deformer)
        {
            this.resolution = resolution;
            Rect rect = MathfUtilities.GetAffectRectangle(chunk, terrainPosition, deformer.AxisAlignedRect);
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
