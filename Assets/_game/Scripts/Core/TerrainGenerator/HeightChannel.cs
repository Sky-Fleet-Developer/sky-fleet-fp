using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class HeightChannel : DeformationChannel<float[,]>
    {
        public TerrainData terrainData { get; private set; }
        private Dictionary<int, List<HeightMapDeformerModule>> deformers = new  Dictionary<int, List<HeightMapDeformerModule>>();
        private int maxDeformerLayer;
        public int SideSize { get; }

        public HeightChannel(TerrainData terrainData, int sizeSide, Vector2Int chunk, string path) : base(chunk, terrainData.size.x)
        {
            this.terrainData = terrainData;
            SideSize = sizeSide;
            deformationLayersCache.Add(new float[sizeSide, sizeSide]);
            if (path != null) RawReader.ReadRaw16(path, deformationLayersCache[0]);
            IsReady = true;
        }

        protected override void ApplyDeformer(IDeformer deformer)
        {
            HeightMapDeformerModule deformerModule = deformer.GetModules<HeightMapDeformerModule>();
            if (deformerModule == null) return;
            AddDeformer(deformerModule, deformer.Layer);
            maxDeformerLayer = Mathf.Max(maxDeformerLayer, deformer.Layer);
            deformerModule.WriteToChannel(this);
            
            RecalculateMatches(deformer);
            ApplyToTerrain();
        }

        private void RecalculateMatches(IDeformer deformer)
        {
            for (int i = deformer.Layer + 1; i <= maxDeformerLayer; i++)
            {
                if (deformers.TryGetValue(i, out List<HeightMapDeformerModule> ds))
                {
                    foreach (HeightMapDeformerModule d in ds)
                    {
                        d.WriteToChannel(this);
                    }
                }
            }
        }

        private void AddDeformer(HeightMapDeformerModule deformer, int layer)
        {
            if (!deformers.ContainsKey(layer))
            {
                deformers.Add(layer, new List<HeightMapDeformerModule>());
            }
            deformers[layer].Add(deformer);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            new RectangleAffectSettings(terrainData, Position, terrainData.heightmapResolution, deformer);

        protected override void ApplyToTerrain()
        {
            if(deformationLayersCache[0] == null) return;
            
            terrainData.SetHeights(0, 0, deformationLayersCache[deformationLayersCache.Count-1]);
        }
        
        public float[,] GetSourceLayer(IDeformer deformer)
        {
            return deformationLayersCache[GetPreviousLayerIdx(deformer.Layer)];
        }

        public float[,] GetDestinationLayer(IDeformer deformer)
        {
            int prev = GetPreviousLayerIdx(deformer.Layer);
            if (deformationLayersCache.Count == prev + 1)
            {
                deformationLayersCache.Add(deformationLayersCache[prev].Clone() as float[,]);   
            }
            return deformationLayersCache[prev + 1];
        }

        private int GetPreviousLayerIdx(int idx)
        {
            return Mathf.Max(0, Mathf.Min(deformationLayersCache.Count, idx));
        }
    }
}
