using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
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
        [JsonRequired] private string _configuration;
        [JsonRequired] private string _head;
        
        public StructureBundle()
        {
        }

        public StructureBundle(StructureConfigurationHead head, IEnumerable<Configuration<IStructure>> configs, ISerializationContext context)
        {
            _head = JsonConvert.SerializeObject(head);
            _configuration = JsonConvert.SerializeObject(configs);
        }
        
        public StructureBundle(IStructure structure, ISerializationContext context)
        {
            structure.Init();
            
            Transform tr = structure.transform;
            
            _configuration = JsonConvert.SerializeObject(new BlocksConfiguration(structure));
            name = tr.name;

            
            foreach (IBlock block in structure.Blocks!)
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
            var head = JsonConvert.DeserializeObject<StructureConfigurationHead>(_head);
            RemotePrefabItem item = TablePrefabs.Instance.GetItem(head.bodyGuid);
            GameObject prefab = await item.LoadPrefab();
            if (prefab == null) return null;

            IStructure instance = Object.Instantiate(prefab).GetComponent<IStructure>();
            instance.transform.name = name;

            List<Configuration<IStructure>> config = JsonConvert.DeserializeObject<List<Configuration<IStructure>>>(_configuration);
            List<Task> waiting = new List<Task>();
            foreach (Configuration<IStructure> configuration in config)
            {
                waiting.Add(configuration.Apply(instance));
            }
            
            await Task.WhenAll(waiting);
            instance.Init();

            Transform tr = instance.transform;
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
