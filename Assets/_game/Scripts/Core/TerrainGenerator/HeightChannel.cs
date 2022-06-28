using System;
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
    public class HeightChannel : DeformationChannel<float[,], HeightMapDeformerModule>
    {
        public TerrainData terrainData { get; private set; }
        public int SideSize { get; }

        public HeightChannel(TerrainData terrainData, int sizeSide, Vector2Int chunk, string path) : base(chunk, terrainData.size.x)
        {
            this.terrainData = terrainData;
            SideSize = sizeSide;
            deformationLayersCache.Add(new float[sizeSide, sizeSide]);
            if (path != null) RawReader.ReadRaw16(path, deformationLayersCache[0]);
            IsReady = true;
        }

        protected override void ApplyToCache(HeightMapDeformerModule module)
        {
            module.WriteToChannel(this);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            new RectangleAffectSettings(terrainData, Position, terrainData.heightmapResolution, deformer);

        protected override void ApplyToTerrain()
        {
            if(deformationLayersCache[0] == null) return;
            
            terrainData.SetHeights(0, 0, GetLastLayer());
        }

        protected override float[,] GetLayerCopy(float[,] source)
        {
            return source.Clone() as float[,];
        }
    }
}
