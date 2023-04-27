using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Graph.Wires;

namespace Core.ContentSerializer.CustomSerializers
{
    public class PortSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            Port port = (Port) source;
            return port.Guid;
        }

        public int GetStringsCount() => 1;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            Port port = (Port) source;
            port.SetGuid(cache[prefix]);
            return Task.CompletedTask;
        }
    }
}
