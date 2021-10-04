using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ContentSerializer
{
    public class MaterialCreator : IAssetCreator
    {
        public async Task<Object> CreateInstance(string prefix, Dictionary<string, string> hash, ISerializationContext context)
        {
            return new Material(Shader.Find(hash[prefix + "_1"]));
        }
    }
}
