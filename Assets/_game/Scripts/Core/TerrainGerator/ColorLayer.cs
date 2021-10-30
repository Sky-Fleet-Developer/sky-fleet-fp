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

        public ColorLayer(int sizeSide, int x, int y, string path) : base(x, y)
        {

            ColorTexture = new Texture2D((int)sizeSide, (int)sizeSide);
            PNGReader.ReadPNG(path, ColorTexture);
        }

        public override void ApplyDeformer(IDeformer deformer)
        {
            
        }

        public override void ApplyToTerrain(Terrain data)
        {
            
        }
    }
}