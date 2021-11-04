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
        public Vector2Int Chunk { get; }
        public Vector3 Position { get; }

        public TerrainLayer(Vector2Int chunk, float chunkSize)
        {
            Chunk = chunk;
            Position = new Vector3(chunk.x * chunkSize, 0, chunk.y * chunkSize);
        }
        
        public void RegisterDeformer(IDeformer deformer)
        {
            ApplyDeformer(deformer);
        }

        protected abstract void ApplyDeformer(IDeformer deformer);

        public abstract void ApplyToTerrain();
        
        [ShowInInspector] public bool IsReady { get; protected set; }
    }
}