using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer.ResourceSerializer
{
    [Serializable]
    public class AssetBundle : Bundle
    {
        public string type;
        [NonSerialized] public ISerializationContext context;
        [ShowInInspector] public Dictionary<string, string> Cache;

        public AssetBundle()
        {
            
        }
        
        public AssetBundle(Object asset, ISerializationContext context)
        {
            name = asset.name;
            type = asset.GetType().FullName;
            id = asset.GetInstanceID();
            this.context = context;
            Cache = new Dictionary<string, string>();
            CacheService.GetNestedCache(type, asset, Cache, context);
        }
    }
}
