using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator
{
    public class ColorLayer : TerrainLayer
    {
        public Texture2D ColorTexture;

        public ColorLayer(int sizeSide, int x, int y, string path) : base(sizeSide, x, y)
        {

            ColorTexture = new Texture2D((int)SizeSide, (int)SizeSide);
            PNGReader.ReadPNG(path, ColorTexture);
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