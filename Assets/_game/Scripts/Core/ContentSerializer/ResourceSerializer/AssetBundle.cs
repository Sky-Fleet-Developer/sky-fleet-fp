using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentSerializer
{
    [Serializable]
    public class AssetBundle
    {
        public string name;
        public int id;
        [NonSerialized] public ISerializationContext context;
        [ShowInInspector] public Dictionary<string, string> Hash;

        public AssetBundle()
        {
        }
        
        public AssetBundle(Object asset, ISerializationContext context)
        {
            name = asset.GetType().Name;
            id = asset.GetInstanceID();
            this.context = context;
            Hash = new Dictionary<string, string>();
            HashService.GetNestedHash(asset.GetType().Name, asset, Hash, context);
        }
    }
}
