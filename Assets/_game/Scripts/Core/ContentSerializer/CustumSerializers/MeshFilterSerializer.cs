using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.ContentSerializer.CustumSerializers
{
    public class MeshFilterSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            var mf = (MeshFilter) source;

            context.DetectedObjectReport(mf.sharedMesh);

            return JsonConvert.SerializeObject(mf.sharedMesh.GetInstanceID());
        }

        public int GetStringsCount() => 1;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            var id = JsonConvert.DeserializeObject<int>(hash[prefix]);
            var obj = context.GetObject(id);
            var mf = (MeshFilter) source;
            mf.sharedMesh = (Mesh) obj;
            return Task.CompletedTask;
        }
    }
}