using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer.Bundles;
using Core.Structure;
using UnityEngine;
using AssetBundle = Core.ContentSerializer.Bundles.AssetBundle;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer
{
    public class Serializer : ISerializationContext
    {
        public System.Action<Object> DetectedObjectReport { get; set; }
        public System.Action<string> AddTag { get; set; }
        public System.Func<int, Task<Object>> GetObject => throw new System.NotImplementedException();
        public Assembly[] AvailableAssemblies => throw new System.NotImplementedException();
        public System.Type GetTypeByName(string name)
        {
            throw new System.NotImplementedException();
        }

        public SerializerBehaviour Behaviour { get; }
        public string ModFolderPath { get; set; }
        public bool IsCurrentlyBuilded { get; set; }


        public Serializer(SerializerBehaviour behaviour)
        {
            Behaviour = behaviour;
            Behaviour.context = this;
        }

        public List<PrefabBundle> GetBundlesFor(IEnumerable<GameObject> prefabs)
        {
            return prefabs.Select(x => new PrefabBundle(x, this)).ToList();
        }
        
        public List<AssetBundle> GetBundlesFor(IEnumerable<Object> prefabs)
        {
            return prefabs.Where(x => x != null).Select(x => new AssetBundle(x, this)).ToList();
        }

        public List<StructureBundle> GetBundlesFor(IEnumerable<IStructure> structures)
        {
            return structures.Select(x => new StructureBundle(x, this)).ToList();
        }
    }
}