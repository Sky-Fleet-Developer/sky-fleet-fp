using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class PortSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            var port = (Port) source;
            return port.Guid;
        }

        public int GetStringsCount() => 1;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> hash, ISerializationContext context)
        {
            var port = (Port) source;
            port.SetGUID(hash[prefix]);
            return Task.CompletedTask;
        }
    }
}
