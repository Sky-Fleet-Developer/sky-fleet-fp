using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace ContentSerializer
{
    public class Serializer : ISerializationContext
    {
        public System.Action<UnityEngine.Object> DetectedObjectReport { get; set; }
        public System.Func<int, Object> GetObject => throw new System.NotImplementedException();
        public SerializerBehaviour Behaviour { get; }

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
            return prefabs.Select(x => new AssetBundle(x, this)).ToList();
        }
    }
}