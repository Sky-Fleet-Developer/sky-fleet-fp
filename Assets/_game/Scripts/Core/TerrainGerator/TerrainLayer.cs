using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator
{

    [System.Serializable]
    public class LayerSettings
    {

    }

    public abstract class TerrainLayer
    {
        public Vector2Int Pos { get; private set; }

       

        public TerrainLayer(int x, int y)
        {
            Pos = new Vector2Int(x, y);
        }

        public abstract void ApplyDeformer(IDeformer deformer);

        public abstract void ApplyToTerrain(Terrain data);
    }
}