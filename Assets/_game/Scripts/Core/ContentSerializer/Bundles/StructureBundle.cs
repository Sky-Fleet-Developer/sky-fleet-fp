using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.ContentSerializer.Bundles
{
    [System.Serializable]
    public class StructureBundle : Bundle
    {
        [JsonRequired] private readonly Dictionary<string, string> blocksCache = new Dictionary<string, string>();
        [JsonRequired] private readonly Dictionary<string, string> parentsCache = new Dictionary<string, string>();
        [JsonRequired] private readonly Dictionary<string, string> dynamicsCache = new Dictionary<string, string>();
        [JsonRequired] private string configuration;
        [JsonRequired] private string guid;
        
        public StructureBundle()
        {
            
        }
        
        public StructureBundle(IStructure structure, ISerializationContext context)
        {
            if(structure.Blocks == null || structure.Blocks.Count == 0) structure.RefreshBlocksAndParents();

            configuration = structure.Configuration;
            guid = structure.Guid;

            var tr = structure.transform;
            
            foreach (var block in structure.Blocks)
            {
                context.Behaviour.GetNestedCache(block.Guid, block, blocksCache);
            }
            
            context.Behaviour.GetNestedCache("this", tr, parentsCache);
            foreach (var parent in structure.Parents)
            {
                context.Behaviour.GetNestedCache(parent.Transform.name, parent.Transform, parentsCache);
            }

            foreach (var rigidbody in tr.GetComponentsInChildren<Rigidbody>())
            {
                string prefix = rigidbody.transform == tr ? "this" : rigidbody.name;
                context.Behaviour.GetNestedCache(prefix, rigidbody, dynamicsCache);
            }
        }

        public async Task<IStructure> ConstructStructure(ISerializationContext context)
        {
            var item = TablePrefabs.Instance.GetItem(guid);
            var prefab = await item.LoadPrefab();
            if (prefab == null) return null;

            var instance = Object.Instantiate(prefab).GetComponent<IStructure>();
            instance.Configuration = configuration;
            if (Application.isPlaying)
            {
                instance.Init();
            }
            else
            {
                instance.RefreshBlocksAndParents();
            }

            var tr = instance.transform;

            List<Task> awaiters = new List<Task>();
            Task tempTask;
            foreach (var block in instance.Blocks)
            {
                tempTask = context.Behaviour.SetNestedCache(block.Guid, block, blocksCache, null);
                awaiters.Add(tempTask);
            }
            
            tempTask = context.Behaviour.SetNestedCache("this", tr, parentsCache, null);
            awaiters.Add(tempTask);
            
            foreach (var parent in instance.Parents)
            {
                tempTask = context.Behaviour.SetNestedCache(parent.Transform.name, parent.Transform, parentsCache, null);
                awaiters.Add(tempTask);
            }
            
            foreach (var rigidbody in tr.GetComponentsInChildren<Rigidbody>())
            {
                string prefix = rigidbody.transform == tr ? "this" : rigidbody.name;
                tempTask = context.Behaviour.SetNestedCache(prefix, rigidbody, dynamicsCache, null);
                awaiters.Add(tempTask);
            }

            await Task.WhenAll(awaiters);
            return instance;
        }
    }
}
