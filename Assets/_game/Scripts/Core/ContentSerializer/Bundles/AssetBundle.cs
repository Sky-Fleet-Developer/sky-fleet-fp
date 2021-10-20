using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer.Bundles
{
    [Serializable]
    public class AssetBundle : Bundle
    {
        public string type;
        [ShowInInspector] public Dictionary<string, string> Cache;

        public AssetBundle()
        {
            
        }
        
        public AssetBundle(Object asset, ISerializationContext context)
        {
            name = asset.name;
            type = asset.GetType().FullName;
            id = asset.GetInstanceID();
            Cache = new Dictionary<string, string>();
            context.Behaviour.GetNestedCache(type, asset, Cache);
        }
    }
}
