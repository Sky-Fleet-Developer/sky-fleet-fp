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
    public class HeightLayer : TerrainLayer
    {
        private float[,] baseHeights;
        private float[,] deformatedHeights;
        private TerrainData terrainData;
        private List<HeightMapDeformerSettings> deformers = new List<HeightMapDeformerSettings>();
        public int SideSize { get; }

        public HeightLayer(TerrainData terrainData, int sizeSide, Vector2Int chunk, string path) : base(chunk, terrainData.size.x)
        {
            if (path == null)
            {
                IsReady = true;
                return;
            }

            this.terrainData = terrainData;
            SideSize = sizeSide;
            baseHeights = new float[sizeSide, sizeSide];
            RawReader.ReadRaw16(path, baseHeights);
            IsReady = true;
        }

        protected override void ApplyDeformer(IDeformer deformer)
        {
            HeightMapDeformerSettings deformerSettings = deformer.Settings.FirstOrDefault(x => x.GetType() == typeof(HeightMapDeformerSettings)) as HeightMapDeformerSettings;
            if (deformerSettings == null) return;
            deformers.Add(deformerSettings);
            RectangleAffectSettings rectangleSettings = new RectangleAffectSettings(terrainData, Position, terrainData.heightmapResolution, deformer);
            deformerSettings.CalculateCache(this, rectangleSettings, Position, terrainData.size);
        }

        protected override void ApplyToTerrain()
        {
            if (baseHeights == null) return;
            
            if (DeformersDirty)
            {
                BakeDeformations();
            }
            
            terrainData.SetHeights(0, 0, baseHeights);
        }

        private void BakeDeformations()
        {
            var cachedDeformers = new Dictionary<Vector2Int, HeightCache>[deformers.Count];

            for (var i = 0; i < deformers.Count; i++)
            {
                cachedDeformers[i] = deformers[i].cache[this];
            }

            for (int x = 0; x < SideSize; x++)
            {
                for (int y = 0; y < SideSize; y++)
                {
                    float hBase = baseHeights[y, x];
                    
                }
            }
        }
    }
}
