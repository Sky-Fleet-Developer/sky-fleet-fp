using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace ContentSerializer
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

        public void Deserialize(string prefix, ref object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            var id = JsonConvert.DeserializeObject<int>(hash[prefix]);
            var obj = context.GetObject(id);
            var mf = (MeshFilter) source;
            mf.sharedMesh = (Mesh) obj;
        }
    }
}