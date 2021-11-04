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
                    Vector3 pos = Core.transform.rotation * new Vector3((i / (Resolution.x - 1f) - 0.5f) * Core.LocalRect.z + Core.LocalRect.x, 6000, (i2 / (Resolution.y - 1f) - 0.5f) * Core.LocalRect.w + Core.LocalRect.y);
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
                int resolution = terrain.terrainData.heightmapResolution;
                Rect rect = GetAffectRectangle(terrain);
                int minX = Mathf.CeilToInt(rect.x * resolution);
                int minY = Mathf.CeilToInt(rect.y * resolution);
                minX = Mathf.Max(minX, 0);
                minY = Mathf.Max(minY, 0);
                int maxX = Mathf.FloorToInt(rect.xMax * resolution);
                int maxY = Mathf.FloorToInt(rect.yMax * resolution);
                maxX = Mathf.Min(maxX, resolution);
                maxY = Mathf.Min(maxY, resolution);
                int deltaX = maxX - minX;
                int deltaY = maxY - minY;
                
                float[,] heights = terrain.terrainData.GetHeights(minX, minY, deltaX, deltaY);

                Rect globalRect = Core.AxisAlignedRect;
                float hMid = Core.transform.position.y;
                float heightInv = 1f / terrain.terrainData.size.y;

                Vector3 terrPos = terrain.transform.position;
                float ceilSize = terrain.terrainData.size.x / resolution;
                
                for (int x = 0; x < deltaX; x++)
                {
                    for (int y = 0; y < deltaY; y++)
                    {
                        Vector3 worldPos = terrPos + new Vector3((minX + x) * ceilSize, 0, (minY + y) * ceilSize); //new Vector3(globalRect.x + globalRect.width * (x / (deltaX - 1f)), 0, globalRect.y + globalRect.height * (y / (deltaY - 1f)));

                        Vector2 localCoordinates = GetLocalPointCoordinates(worldPos);
                        
                        if(localCoordinates.x < 0 || localCoordinates.x > 1 || localCoordinates.y < 0 || localCoordinates.y > 1) continue;

                        float hAdd = GetHeightBilinear(localCoordinates.x, localCoordinates.y);
                        float hDelta = (hMid + hAdd) * heightInv - heights[y, x];

                        #if UNITY_EDITOR
                        Debug.DrawLine(worldPos + Vector3.up * heights[y, x] / heightInv, worldPos + Vector3.up * (hMid + hAdd), hDelta < 0 ? Color.green : Color.red, 5);
                        #endif

                        heights[y, x] += hDelta * GetOpacity(localCoordinates.x, localCoordinates.y);
                    }
                }
#if UNITY_EDITOR
Undo.RecordObject(terrain.terrainData, "change terrain");
#endif
                terrain.terrainData.SetHeights(minX, minY, heights);
                
#if UNITY_EDITOR
EditorUtility.SetDirty(terrain.terrainData);
#endif
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

        private Rect GetAffectRectangle(Terrain terrain)
        {
            Vector3 pos = terrain.transform.position;
            Rect result = Core.AxisAlignedRect;
            result.position -= pos.XZ();

            float terrainSizeInv = 1f / terrain.terrainData.size.x;

            result.position *= terrainSizeInv;
            result.size *= terrainSizeInv;

            return result;
        }
    }
}