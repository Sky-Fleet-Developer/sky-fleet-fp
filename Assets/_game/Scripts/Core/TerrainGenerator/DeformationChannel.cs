using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Runtime state and management for single chunk
    /// </summary>
    [ShowInInspector]
    public abstract class DeformationChannel<T> : DeformationChannel
    {
        protected List<T> deformationLayersCache = new List<T>();

        protected DeformationChannel(Vector2Int chunk, float chunkSize) : base(chunk, chunkSize)
        {
        }
    }

    [ShowInInspector]
    public abstract class DeformationChannel
    {
        public Vector2Int Chunk { get; }
        public Vector3 Position { get; }
        public Vector3 WorldPosition => WorldOffset.Instance.Offset + Position;

        public DeformationChannel(Vector2Int chunk, float chunkSize)
        {
            Chunk = chunk;
            Position = new Vector3(chunk.x * chunkSize, 0, chunk.y * chunkSize);
        }
        
        public void RegisterDeformer(IDeformer deformer)
        {
            ApplyDeformer(deformer);
        }

        protected abstract void ApplyDeformer(IDeformer deformer);

        public void Apply()
        {
            ApplyToTerrain();
        }
        
        protected abstract void ApplyToTerrain();
        
        [ShowInInspector] public bool IsReady { get; protected set; }

        public abstract RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer);
    }
}
