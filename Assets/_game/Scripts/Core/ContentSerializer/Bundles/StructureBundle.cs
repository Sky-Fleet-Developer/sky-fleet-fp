using System.Collections.Generic;
using System.Threading.Tasks;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
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
            
            Transform tr = structure.transform;
            
            configuration = JsonConvert.SerializeObject(new StructureConfiguration(structure));
            guid = structure.Guid;
            name = tr.name;

            
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

        public async Task<BaseStructure> ConstructStructure(ISerializationContext context)
        {
            RemotePrefabItem item = TablePrefabs.Instance.GetItem(guid);
            GameObject prefab = await item.LoadPrefab();
            if (prefab == null) return null;

            BaseStructure instance = Object.Instantiate(prefab).GetComponent<BaseStructure>();
            instance.transform.name = name;
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

            List<Task> waiting = new List<Task>();
            Task tempTask;
            foreach (IBlock block in instance.Blocks)
            {
                tempTask = context.Behaviour.SetNestedCache(block.Guid + block.transform.name, block, blocksCache, null);
                waiting.Add(tempTask);
                tempTask = context.Behaviour.SetNestedCache(block.Guid + block.transform.name, block.transform, blocksCache, null);
                waiting.Add(tempTask);
            }
            
            tempTask = context.Behaviour.SetNestedCache("this", tr, parentsCache, null);
            waiting.Add(tempTask);
            
            foreach (Parent parent in instance.Parents)
            {
                tempTask = context.Behaviour.SetNestedCache(parent.Transform.name, parent.Transform, parentsCache, null);
                waiting.Add(tempTask);
            }
            
            foreach (Rigidbody rigidbody in tr.GetComponentsInChildren<Rigidbody>())
            {
                string prefix = rigidbody.transform == tr ? "this" : rigidbody.name;
                tempTask = context.Behaviour.SetNestedCache(prefix, rigidbody, dynamicsCache, null);
                waiting.Add(tempTask);
            }

            await Task.WhenAll(waiting);
            
            Debug.Log($"Structure {instance.transform.name} successfully loaded!");
            return instance;
        }
    }
}
