using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator
{
    public class TreesLayer : TerrainLayer
    {
        public Texture2D TreesTexture;

        public TreesLayer(int sizeSide, int x, int y, string path) : base(x, y)
        {

            TreesTexture = new Texture2D(sizeSide, sizeSide);
            PNGReader.ReadPNG(path, TreesTexture);
        }

        public override void ApplyDeformer(IDeformer deformer)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyToTerrain(Terrain data)
        {
            throw new System.NotImplementedException();
        }
    }
}