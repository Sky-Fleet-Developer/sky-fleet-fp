using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class MeshFilterSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            MeshFilter mf = (MeshFilter) source;

            context.DetectedObjectReport(mf.sharedMesh);

            return JsonConvert.SerializeObject(mf.sharedMesh.GetInstanceID());
        }

        public int GetStringsCount() => 1;

        public async Task Deserialize(string prefix, object source, Dictionary<string, string> cache,
            ISerializationContext context)
        {
            int id = JsonConvert.DeserializeObject<int>(cache[prefix]);
            Object obj = await context.GetObject(id);
            MeshFilter mf = (MeshFilter) source;
            mf.sharedMesh = (Mesh) obj;
        }
    }
}