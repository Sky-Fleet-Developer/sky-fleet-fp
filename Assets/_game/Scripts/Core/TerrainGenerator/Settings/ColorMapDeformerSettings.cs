using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class ColorMapDeformerSettings : IDeformerLayerSetting
    {
        public float[] SplatMaps;
        public Vector2Int Resolution;

        [HideInInspector]
        public int CountLayers;

        [JsonIgnore]
        public Deformer Core { get; set; }



        public void Init(Deformer core)
        {
            Core = core;
        }

        [Button]
        public void ReadFromTerrain()
        {
            Terrain[] terrains = Core.GetTerrainsContacts();
            Vector4 rect = Core.LocalRect;

            CountLayers = (int)Mathf.Max(terrains.Select(x => { return (float)x.terrainData.terrainLayers.Length; }).ToArray());
            SplatMaps = new float[Resolution.x * Resolution.y * CountLayers];
            if (CountLayers == 0) return;

            for (int i = 0; i < Resolution.x; i++)
            {
                for (int i2 = 0; i2 < Resolution.y; i2++)
                {
                    Vector3 pos = Core.transform.rotation * new Vector3((i / (Resolution.x - 1f) - 0.5f) * rect.z + rect.x, 6000, (i2 / (Resolution.y - 1f) - 0.5f) * rect.w + rect.y) + Core.transform.position;

                    Terrain tr = GetTerrainInPos(terrains.ToArray(), pos);
                    Debug.DrawLine(new Vector3(pos.x, Core.transform.position.y, pos.z), new Vector3(pos.x, 0, pos.z), tr != null ? Color.blue : Color.red, 2);
                    if (tr != null)
                    {
                        float[] alphas = GetLayersFromTerrainInPos(tr, pos);
                        for (int i3 = 0; i3 < CountLayers; i3++)
                        {
                            if (i3 >= alphas.Length)
                            {
                                break;
                            }
                            SplatMaps[i3 * Resolution.x * Resolution.y + i * Resolution.x + i2] = alphas[i3];
                        }
                    }
                    else
                    {
                        SplatMaps[0 * Resolution.x * Resolution.y + i * Resolution.x + i2] = 1;
                    }
                }
            }
        }

        private Terrain GetTerrainInPos(Terrain[] terrains, Vector3 pos)
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                Rect rect = new Rect(terrains[i].transform.position, new Vector2(terrains[i].terrainData.size.x, terrains[i].terrainData.size.z));
                if (rect.Contains(new Vector2(pos.x, pos.z)))
                {
                    return terrains[i];
                }
            }
            return null;
        }

        private float[] GetLayersFromTerrainInPos(Terrain terrain, Vector3 pos)
        {
            Vector3 localPos = terrain.transform.InverseTransformPoint(pos);
            Vector2 normalized = new Vector2(localPos.x / terrain.terrainData.size.x, localPos.z / terrain.terrainData.size.z);
            float[,,] alpha = terrain.terrainData.GetAlphamaps((int)(normalized.x * terrain.terrainData.alphamapWidth), (int)(normalized.y * terrain.terrainData.alphamapWidth), 1, 1);
            float[] alphaNormal = new float[alpha.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                alphaNormal[i] = alpha[0, 0, i];
            }
            return alphaNormal;
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
                int resolution = terrain.terrainData.alphamapResolution;
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

                float[,,] alphas = terrain.terrainData.GetAlphamaps(minX, minY, deltaX, deltaY);
                int countTrLayers = terrain.terrainData.terrainLayers.Length;

                Rect globalRect = Core.AxisAlignedRect;
                Vector3 terrPos = terrain.transform.position;
                float ceilSize = terrain.terrainData.size.x / resolution;

                for (int x = 0; x < deltaX; x++)
                {
                    for (int y = 0; y < deltaY; y++)
                    {
                        Vector3 worldPos = terrPos + new Vector3((minX + x) * ceilSize, 0, (minY + y) * ceilSize);

                        Vector2 localCoordinates = GetLocalPointCoordinates(worldPos);

                        if (localCoordinates.x < 0 || localCoordinates.x > 1 || localCoordinates.y < 0 || localCoordinates.y > 1) continue;

                        float[] alphasR = GetAlphasBilinear(localCoordinates.x, localCoordinates.y);
                        float opacity = GetOpacity(localCoordinates.x, localCoordinates.y);
                        for (int i = 0; i < alphasR.Length; i++)
                        {
                            if (i >= countTrLayers) break;

                            alphas[y, x, i] = alphas[y, x, i] * (1 - opacity) + alphasR[i] * opacity;
                        }

#if UNITY_EDITOR
                        Debug.DrawLine(worldPos + Vector3.up * Core.transform.position.y, worldPos, Color.red, 2);
#endif
                    }
                }
#if UNITY_EDITOR
                Undo.RecordObject(terrain.terrainData, "change terrain");
#endif
                terrain.terrainData.SetAlphamaps(minX, minY, alphas);

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

        private float[] GetAlphasBilinear(float x, float y)
        {
            float xPos = x * Resolution.x;
            float yPos = y * Resolution.y;

            int minX = Mathf.Min(Mathf.FloorToInt(xPos), Resolution.x - 2);
            int minY = Mathf.Min(Mathf.FloorToInt(yPos), Resolution.y - 2);

            float remainsX = xPos - minX;
            float remainsY = yPos - minY;

            float[] layers = new float[CountLayers];
            for (int i = 0; i < CountLayers; i++)
            {

                float topValue = Mathf.LerpUnclamped(SplatMaps[i * Resolution.x * Resolution.y + minX * Resolution.y + minY],
                    SplatMaps[i * Resolution.x * Resolution.y + minX * Resolution.y + Resolution.y + minY], remainsX);

                float bottomValue = Mathf.LerpUnclamped(SplatMaps[i * Resolution.x * Resolution.y + minX * Resolution.y + minY + 1],
                    SplatMaps[i * Resolution.x * Resolution.y + minX * Resolution.y + Resolution.y + minY + 1], remainsX);
                layers[i] = Mathf.LerpUnclamped(topValue, bottomValue, remainsY);
            }
            return layers;
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