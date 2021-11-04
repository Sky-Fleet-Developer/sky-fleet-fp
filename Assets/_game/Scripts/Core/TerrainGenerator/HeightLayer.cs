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
        private TerrainData terrainData;
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
            RectangleAffectSettings rectangleSettings = new RectangleAffectSettings(terrainData, Position, terrainData.heightmapResolution, deformer);

            var heights = terrainData.GetHeights(rectangleSettings.minX, rectangleSettings.minY,
                rectangleSettings.deltaX, rectangleSettings.deltaY);
            
            deformerSettings.WriteToHeightmap(heights, 0, 0, rectangleSettings, Position, terrainData.size);
            
            terrainData.SetHeights(rectangleSettings.minX, rectangleSettings.minY, heights);
        }

        public override void ApplyToTerrain()
        {
            if (baseHeights == null) return;

            terrainData.SetHeights(0, 0, baseHeights);
        }
    }
}