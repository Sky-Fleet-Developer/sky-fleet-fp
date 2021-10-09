using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class IBlockSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            var block = (IBlock) source;
            return block.Save();
        }

        public int GetStringsCount() => 1;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            var block = (IBlock) source;
            block.Load(cache[prefix]);
            return Task.CompletedTask;
        }
    }
}
