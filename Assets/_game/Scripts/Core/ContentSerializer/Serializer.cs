using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.ContentSerializer.HierarchySerializer;
using UnityEngine;
using AssetBundle = Core.ContentSerializer.ResourceSerializer.AssetBundle;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer
{
    public class Serializer : ISerializationContext
    {
        public System.Action<Object> DetectedObjectReport { get; set; }
        public System.Action<string> AddTag { get; set; }
        public System.Func<int, Object> GetObject => throw new System.NotImplementedException();
        public Assembly[] AvailableAssemblies => throw new System.NotImplementedException();
        public System.Type GetTypeByName(string name)
        {
            throw new System.NotImplementedException();
        }

        public SerializerBehaviour Behaviour { get; }
        public string ModFolderPath => throw new System.NotImplementedException();
        public bool IsCurrentlyBuilded { get; set; }


        public Serializer(SerializerBehaviour behaviour)
        {
            Behaviour = behaviour;
            Behaviour.Context = this;
        }

        public List<PrefabBundle> GetBundlesFor(GameObject[] prefabs)
        {
            return prefabs.Select(x => new PrefabBundle(x, this)).ToList();
        }
        
        public List<AssetBundle> GetBundlesFor(Object[] prefabs)
        {
            return prefabs.Where(x => x != null).Select(x => new AssetBundle(x, this)).ToList();
        }
    }
}