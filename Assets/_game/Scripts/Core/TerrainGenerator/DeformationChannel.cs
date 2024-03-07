using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Core.Game;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using Core.Utilities;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Runtime state and management for single chunk
    /// </summary>
    [ShowInInspector]
    public abstract class DeformationChannel<DataT, TModule> : DeformationChannel where TModule : class, IDeformerModule
    {
        protected List<DataT> deformationLayersCache = new List<DataT>();
        protected Dictionary<int, List<TModule>> deformers = new  Dictionary<int, List<TModule>>();
        protected Dictionary<int, List<TModule>> dirtyDeformers = new  Dictionary<int, List<TModule>>();
        protected int maxDeformerLayer;
        
        protected DeformationChannel(Vector2Int coordinates, float chunkSize) : base(coordinates, chunkSize)
        {
        }
        
        protected DataT GetLastLayer() => deformationLayersCache[deformationLayersCache.Count - 1];
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
        
        protected void AddDeformer(TModule deformer)
        {
            int layer = deformer.Core.Layer;
            if (!deformers.ContainsKey(layer))
            {
                deformers.Add(layer, new List<TModule>());
            }
            deformers[layer].Add(deformer);
        }
        
        private void AddDeformerToDirty(TModule deformer)
        {
            int layer = deformer.Core.Layer;
            if (!dirtyDeformers.ContainsKey(layer))
            {
                dirtyDeformers.Add(layer, new List<TModule>());
            }
            dirtyDeformers[layer].Add(deformer);
            IsDirty = true;
            deformer.Core.OnSetDirty(deformer.GetType());
        }

        public override void RegisterDeformer(IDeformer deformer)
        {
            TModule module = deformer.GetModules<TModule>();
            if (module == null) return;
            AddDeformer(module);
            maxDeformerLayer = Mathf.Max(maxDeformerLayer, module.Core.Layer);
            AddDeformerToDirty(module);
            CalculateDirty(deformer);
        }

        public override void ApplyDirtyToCache()
        {
            int count = dirtyDeformers.Count;
            for (int i = 0; i < count; i++)
            {
                foreach (TModule deformerModule in dirtyDeformers[i])
                {
                    //Debug.Log($"Deformer dirty: {deformerModule.GetType().Name} : {(deformerModule.Core as UnityEngine.Object)?.name}");
                    ApplyToCache(deformerModule);
                }

                dirtyDeformers[i].Clear();
            }
        }
        
        protected abstract void ApplyToCache(TModule module);

        protected void CalculateDirty(IDeformer deformer)
        {
            Rect rect = deformer.AxisAlignedRect;
            Type changedModuleType = typeof(TModule);
            int layerToRecalculate = deformer.Layer + 1;
            if (layerToRecalculate > maxDeformerLayer) return;

            if (!dirtyDeformers.ContainsKey(layerToRecalculate))
            {
                dirtyDeformers.Add(layerToRecalculate, new List<TModule>());
            }

            List<TModule> layer = dirtyDeformers[layerToRecalculate];
            
            if (deformers.TryGetValue(layerToRecalculate, out List<TModule> ds))
            {
                foreach (TModule d in ds)
                {
                    Rect dRect = d.Core.AxisAlignedRect;
                    if (dRect.Overlaps(rect))
                    {
                        layer.Add(d);
                        d.Core.OnSetDirty(changedModuleType);
                    }
                }
            }
        }
    }

    [ShowInInspector]
    public abstract class DeformationChannel
    {
        public Vector2Int Coordinates { get; }
        public Vector3 Position { get; }
        public Vector3 WorldPosition => Position - WorldOffset.Offset;
        public bool IsDirty { get; protected set; }

        public DeformationChannel(Vector2Int coordinates, float chunkSize)
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
    }
}
