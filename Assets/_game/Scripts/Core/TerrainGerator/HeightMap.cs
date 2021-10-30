using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator
{
    public class HeightMap : TerrainLayer
    {
        public float[,] Heights;
        
        public int SideSize { get; private set; }

        public HeightMap(int sizeSide, int x, int y, string path) : base(x, y)
        {
            SideSize = sizeSide;
            Heights = new float[sizeSide, sizeSide];
            RawReader.ReadRaw16(path, this);
        }

        public override void ApplyDeformer(IDeformer deformer)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyToTerrain(Terrain data)
        {
            data.terrainData.SetHeights(0, 0, Heights);
        }
    }
}