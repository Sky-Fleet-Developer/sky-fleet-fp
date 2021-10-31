using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class HeightLayer : TerrainLayer
    {
        [System.NonSerialized] public float[,] heights;
        public TerrainData terrainData;
        public int SideSize { get; private set; }

        public HeightLayer(TerrainData terrainData, int sizeSide, Vector2Int position, string path) : base(position)
        {
            if (path == null)
            {
                IsReady = true;
                return;
            }

            this.terrainData = terrainData;
            SideSize = sizeSide;
            heights = new float[sizeSide, sizeSide];
            RawReader.ReadRaw16(path, this);
            IsReady = true;
        }

        public override void ApplyDeformer(IDeformer deformer)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyToTerrain()
        {
            if (heights == null) return;
            terrainData.SetHeights(0, 0, heights);
        }
    }
}