using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using Core.World;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.TerrainGenerator
{
    public abstract class LayeredDeformationChannel<DataT, TModule> :  DeformationChannel<TModule> where TModule : class, IDeformerModule
    {
        protected List<DataT> deformationLayersCache = new List<DataT>();

        protected LayeredDeformationChannel(TerrainProvider terrain, Vector2Int coordinates, float chunkSize) : base(terrain, coordinates, chunkSize)
        {
        }

        protected DataT GetLastLayer() => deformationLayersCache[^1];
        public DataT GetSourceLayer(IDeformer deformer)
        {
            int l = GetPreviousLayerIdx(deformer.Layer);
            var dlc = deformationLayersCache;
            try
            {
                return dlc[l];
            }
            catch (Exception e)
            {
                Debug.LogError(JsonConvert.SerializeObject(dlc));
                throw;
            }
        }

        public IEnumerable<DataT> GetDestinationLayers(IDeformer deformer)
        {
            int prev = GetPreviousLayerIdx(deformer.Layer);
            if (deformationLayersCache.Count == prev + 1)
            {
                deformationLayersCache.Add(GetLayerCopy(deformationLayersCache[prev]));   
            }

            for (int i = prev + 1; i < deformationLayersCache.Count; i++)
            {
                yield return deformationLayersCache[i];
            }
        }

        protected abstract DataT GetLayerCopy(DataT source);
        
        private int GetPreviousLayerIdx(int idx)
        {
            return Mathf.Max(0, Mathf.Min(deformationLayersCache.Count-1, idx));
        }
    }

    [ShowInInspector]
    public abstract class DeformationChannel
    {
        public Vector2Int Coordinates { get; }
        public Vector3 Position { get; }
        public Vector3 WorldPosition => Position - WorldOffset.Offset;
        public bool IsDirty { get; protected set; }

        public DeformationChannel(TerrainProvider terrain, Vector2Int coordinates, float chunkSize)
        {
            Coordinates = coordinates;
            Position = new Vector3(coordinates.x * chunkSize, 0, coordinates.y * chunkSize);
            IsDirty = true;
        }

        public abstract void RegisterDeformer(IDeformer deformer);
        public abstract void ApplyDirtyToCache();

        public async Task Apply()
        {
            if(!LoadingTask.IsCompleted || applyToTerrainTask != null) return;
            applyToTerrainTask = ApplyToTerrain();
            await applyToTerrainTask;
            applyToTerrainTask = null;
            IsDirty = false;
        }

        protected abstract Task ApplyToTerrain();
        public virtual Task PostApply() => Task.CompletedTask;

        protected readonly TaskCompletionSource<bool> loading = new TaskCompletionSource<bool>();
        [ShowInInspector] public Task<bool> LoadingTask => loading.Task;
        private Task applyToTerrainTask = null;
        public abstract RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer);

        public abstract void SetChunk(Chunk chunk);
        
        public virtual void OnChunkLoad(){}
        public virtual void OnChunkUnload(){}
    }
}
