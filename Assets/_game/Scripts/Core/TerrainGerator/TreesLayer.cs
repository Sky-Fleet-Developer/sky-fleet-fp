using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator
{
    public struct TreePos
    {
        public int Layer;
        public Vector2 Pos;
        public int NumTree;

        public TreePos(int layer, int numTree, Vector2 pos)
        {
            Layer = layer;
            NumTree = numTree;
            Pos = pos;
        }
    }

    public class TreesLayer : TerrainLayer
    {
        public List<TreePos> Trees;

        public TreesLayer(int x, int y, string path) : base(x, y)
        {
            Trees = new List<TreePos>();
        }

        public TreesLayer(int x, int y) : base(x, y)
        {
            Trees = new List<TreePos>();
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