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
        [JsonRequired] private Dictionary<string, string> blocksCache = new Dictionary<string, string>();
        [JsonRequired] private Dictionary<string, string> parentsCache = new Dictionary<string, string>();
        [JsonRequired] private Dictionary<string, string> dynamicsCache = new Dictionary<string, string>();
        [JsonRequired] private string configuration;
        [JsonRequired] private string guid;
        
        public StructureBundle()
        {
            
        }
        
        public StructureBundle(IStructure structure, ISerializationContext context)
        {
            if(structure.Blocks == null || structure.Blocks.Count == 0) structure.RefreshBlocksAndParents();

            configuration = JsonConvert.SerializeObject(Factory.GetConfiguration(structure));
            guid = structure.Guid;
            name = structure.transform.name;

            Transform tr = structure.transform;
            
            foreach (IBlock block in structure.Blocks)
            {
                context.Behaviour.GetNestedCache(block.Guid + block.transform.name, block, blocksCache);
                context.Behaviour.GetNestedCache(block.Guid + block.transform.name, block.transform, blocksCache);
            }
            
            context.Behaviour.GetNestedCache("this", tr, parentsCache);
            foreach (Parent parent in structure.Parents)
            {
                context.Behaviour.GetNestedCache(parent.Transform.name, parent.Transform, parentsCache);
            }

            foreach (Rigidbody rigidbody in tr.GetComponentsInChildren<Rigidbody>())
            {
                string prefix = rigidbody.transform == tr ? "this" : rigidbody.name;
                context.Behaviour.GetNestedCache(prefix, rigidbody, dynamicsCache);
            }
        }

        public async Task<IStructure> ConstructStructure(ISerializationContext context)
        {
            RemotePrefabItem item = TablePrefabs.Instance.GetItem(guid);
            GameObject prefab = await item.LoadPrefab();
            if (prefab == null) return null;

            IStructure instance = Object.Instantiate(prefab).GetComponent<IStructure>();
            instance.transform.name = name;
            instance.Configuration = configuration;
            if (Application.isPlaying)
            {
                instance.Init();
            }
            else
            {
                instance.RefreshBlocksAndParents();
                //TODO: instantiate blocks from configuration
            }

            Transform tr = instance.transform;

            List<Task> awaiters = new List<Task>();
            Task tempTask;
            foreach (IBlock block in instance.Blocks)
            {
                tempTask = context.Behaviour.SetNestedCache(block.Guid + block.transform.name, block, blocksCache, null);
                awaiters.Add(tempTask);
                tempTask = context.Behaviour.SetNestedCache(block.Guid + block.transform.name, block.transform, blocksCache, null);
                awaiters.Add(tempTask);
            }
            
            tempTask = context.Behaviour.SetNestedCache("this", tr, parentsCache, null);
            awaiters.Add(tempTask);
            
            foreach (Parent parent in instance.Parents)
            {
                tempTask = context.Behaviour.SetNestedCache(parent.Transform.name, parent.Transform, parentsCache, null);
                awaiters.Add(tempTask);
            }
            
            foreach (Rigidbody rigidbody in tr.GetComponentsInChildren<Rigidbody>())
            {
                string prefix = rigidbody.transform == tr ? "this" : rigidbody.name;
                tempTask = context.Behaviour.SetNestedCache(prefix, rigidbody, dynamicsCache, null);
                awaiters.Add(tempTask);
            }

            await Task.WhenAll(awaiters);
            
            Debug.Log($"Structure {instance.transform.name} successfully loaded!");
            return instance;
        }
    }
}
