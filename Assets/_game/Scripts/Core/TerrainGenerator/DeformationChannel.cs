using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Runtime state and management for single chunk
    /// </summary>
    [ShowInInspector]
    public abstract class DeformationChannel<TModule> : DeformationChannel where TModule : class, IModifier
    {
        protected Dictionary<int, List<TModule>> deformers = new  Dictionary<int, List<TModule>>();
        protected Dictionary<int, List<TModule>> dirtyDeformers = new  Dictionary<int, List<TModule>>();
        protected int maxDeformerLayer = -1;

        protected DeformationChannel(TerrainProvider terrain, Vector2Int coordinates, float chunkSize) : base(terrain, coordinates, chunkSize)
        {
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
            deformer.Core.OnSetDirty(deformer);
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
            int layerToRecalculate = deformer.Layer;
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
                        d.Core.OnSetDirty(d);
                    }
                }
            }
        }
    }
}