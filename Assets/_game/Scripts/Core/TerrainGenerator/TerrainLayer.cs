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
        public bool DeformersDirty { get; private set; }
        
        public void RegisterDeformer(IDeformer deformer)
        {
            DeformersDirty = true;
            ApplyDeformer(deformer);
        }

        protected abstract void ApplyDeformer(IDeformer deformer);

        public void Apply()
        {
            ApplyToTerrain();
            DeformersDirty = false;
        }
        
        protected abstract void ApplyToTerrain();
        
        [ShowInInspector] public bool IsReady { get; protected set; }
    }
}