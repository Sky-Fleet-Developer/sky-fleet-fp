using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public abstract class TerrainLayer
    {
        public Vector2Int Porition { get; private set; }

        public TerrainLayer(Vector2Int position)
        {
            Porition = position;
        }

        public abstract void ApplyDeformer(IDeformer deformer);

        public abstract void ApplyToTerrain();
        
        [ShowInInspector] public bool IsReady { get; protected set; }
    }
}