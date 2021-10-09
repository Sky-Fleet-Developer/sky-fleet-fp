using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.ContentSerializer.AssetCreators
{
    public class MaterialCreator : IAssetCreator
    {
        public async Task<Object> CreateInstance(string prefix, Dictionary<string, string> cache, ISerializationContext context)
        {
            return new Material(Shader.Find(cache[prefix + "_1"]));
        }
    }
}
