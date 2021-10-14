using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class TransformSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            Transform tr = (Transform) source;
            switch (idx)
            {
                default:
                    return JsonConvert.SerializeObject(tr.localPosition, new VectorConverter());
                case 1:
                    return JsonConvert.SerializeObject(tr.localEulerAngles, new VectorConverter());
                case 2:
                    return JsonConvert.SerializeObject(tr.localScale, new VectorConverter());
            }
        }

        public int GetStringsCount() => 3;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            Transform tr = (Transform) source;
            tr.localPosition = JsonConvert.DeserializeObject<Vector3>(cache[prefix]);
            tr.localEulerAngles = JsonConvert.DeserializeObject<Vector3>(cache[prefix + "_1"]);
            tr.localScale = JsonConvert.DeserializeObject<Vector3>(cache[prefix + "_2"]);
            return Task.CompletedTask;
        }
    }
}
