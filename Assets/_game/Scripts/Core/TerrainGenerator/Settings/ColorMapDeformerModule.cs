using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class ColorMapDeformerModule : IDeformerModule
    {
        public float[,,] SplatMaps;
        public Vector2Int Resolution;

        [HideInInspector]
        public int CountLayers;

        [JsonIgnore]
        public IDeformer Core { get; set; }
        
        public void Init(IDeformer core)
        {
            Core = core;
        }

        [Button]
        public void ReadFromTerrain()
        {
            Terrain[] terrains = Core.GetTerrainsContacts();
            Vector4 rect = Core.LocalRect;

            CountLayers = terrains.Max(x => x.terrainData.terrainLayers.Length);
            SplatMaps = new float[CountLayers, Resolution.x, Resolution.y];
            if (CountLayers == 0) return;

            for (int x = 0; x < Resolution.x; x++)
            {
                for (int y = 0; y < Resolution.y; y++)
                {
                    Vector3 pos = Core.Rotation * new Vector3((x / (Resolution.x - 1f) - 0.5f) * rect.z + rect.x, 0, (y / (Resolution.y - 1f) - 0.5f) * rect.w + rect.y) + Core.Position;

                    Terrain tr = GetTerrainInPos(terrains, pos);
                    Debug.DrawLine(new Vector3(pos.x, Core.Position.y, pos.z), new Vector3(pos.x, 0, pos.z), tr != null ? Color.blue : Color.red, 2);
                    if (tr != null)
                    {
                        float[] alphas = GetLayersFromTerrainInPos(tr, pos);
                        for (int idx = 0; idx < CountLayers; idx++)
                        {
                            if (idx >= alphas.Length)
                            {
                                break;
                            }
                            SplatMaps[idx,x,y] = alphas[idx];
                        }
                    }
                    else
                    {
                        SplatMaps[0, x, y] = 1;
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

       /* [Button]
        public void WriteToTerrain()
        {
            WriteToChannel(Core.GetTerrainsContacts());
        }*/

        public void WriteToChannel(DeformationChannel sourceChannel)
        {
            if (!(sourceChannel is ColorChannel channel)) return;
            
            RectangleAffectSettings settings = channel.GetAffectSettingsForDeformer(Core);

                float[,,] alphas = channel.terrainData.GetAlphamaps(settings.minX, settings.minY, settings.deltaX, settings.deltaY);

                int layersCount = channel.terrainData.terrainLayers.Length;

                WriteToAlphamaps(alphas, 0, 0, settings, channel.Position, channel.terrainData.size, layersCount);
#if UNITY_EDITOR
                Undo.RecordObject(channel.terrainData, "change terrain");
#endif
            channel.terrainData.SetAlphamaps(settings.minX, settings.minY, alphas);

#if UNITY_EDITOR
                EditorUtility.SetDirty(channel.terrainData);
#endif
        }

        public void WriteToAlphamaps(float[,,] alphamap, int xBegin, int yBegin, RectangleAffectSettings settings,
            Vector3 terrainPosition, Vector3 terrainSize, int layersCount)
        {
            float ceilSize = terrainSize.x / settings.resolution;

            for (int x = 0; x < settings.deltaX; x++)
            {
                for (int y = 0; y < settings.deltaY; y++)
                {
                    Vector3 worldPos = terrainPosition + new Vector3((settings.minX + x) * ceilSize, 0, (settings.minY + y) * ceilSize);

                    Vector2 localCoordinates = Core.GetLocalPointCoordinates(worldPos);

                    if (localCoordinates.x < 0 || localCoordinates.x > 1 || localCoordinates.y < 0 || localCoordinates.y > 1) continue;

                    float[] alphasR = GetAlphasBilinear(localCoordinates.x, localCoordinates.y);
                    float opacity = GetOpacity(localCoordinates.x, localCoordinates.y);
                    for (int i = 0; i < alphasR.Length; i++)
                    {
                        if (i >= layersCount) break;

                        alphamap[yBegin + y, xBegin + x, i] = alphamap[yBegin + y, xBegin + x, i] * (1 - opacity) + alphasR[i] * opacity;
                    }

#if UNITY_EDITOR
                    Debug.DrawLine(worldPos + Vector3.up * Core.Position.y, worldPos, Color.red, 2);
#endif
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
                float topValue = Mathf.LerpUnclamped(SplatMaps[i, minX, minY],
                    SplatMaps[i, minX + 1, minY], remainsX);

                float bottomValue = Mathf.LerpUnclamped(SplatMaps[i, minX, minY + 1],
                    SplatMaps[i, minX + 1, minY + 1], remainsX);
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