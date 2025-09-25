using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class RigidbodySerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            Rigidbody rb = (Rigidbody) source;
            switch (idx)
            {
                default:
                    return JsonConvert.SerializeObject(rb.linearVelocity, new VectorConverter());
                case 1:
                    return JsonConvert.SerializeObject(rb.angularVelocity, new VectorConverter());
                case 2:
                    return JsonConvert.SerializeObject(rb.mass);
                case 3:
                    return JsonConvert.SerializeObject(rb.linearDamping);
                case 4:
                    return JsonConvert.SerializeObject(rb.angularDamping);
            }
        }

        public int GetStringsCount() => 5;

        public Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            Rigidbody rb = (Rigidbody) source;
            rb.linearVelocity = JsonConvert.DeserializeObject<Vector3>(cache[prefix]);
            rb.angularVelocity = JsonConvert.DeserializeObject<Vector3>(cache[prefix + "_1"]);
            rb.mass = JsonConvert.DeserializeObject<float>(cache[prefix + "_2"]);
            rb.linearDamping = JsonConvert.DeserializeObject<float>(cache[prefix + "_3"]);
            rb.angularDamping = JsonConvert.DeserializeObject<float>(cache[prefix + "_4"]);
            return Task.CompletedTask;
        }
    }
}
