using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.ContentSerializer.CustomSerializers
{
    public class MeshRendererSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            MeshRenderer mr = (MeshRenderer) source;
            
            for (int i = 0; i < mr.sharedMaterials.Length; i++)
            {
                context.DetectedObjectReport(mr.sharedMaterials[i]);
            }

            var serializedSource = new SerializedMeshRenderer(mr);
            return JsonConvert.SerializeObject(serializedSource);
        }
        
        public int GetStringsCount() => 1;

        public async Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            MeshRenderer mr = (MeshRenderer) source;
            var hashValue = cache[prefix];
            var serializedSource = JsonConvert.DeserializeObject<SerializedMeshRenderer>(hashValue);
            var mats = new Material[serializedSource.materials.Length];
            for (var i = 0; i < mats.Length; i++)
            {
                mats[i] = (Material) await context.GetObject(serializedSource.materials[i]);
            }
            mr.materials = mats;
            mr.shadowCastingMode = serializedSource.shadowCastingMode;
            mr.receiveShadows = serializedSource.receiveShadows;
            mr.lightProbeUsage = serializedSource.lightProbeUsage;
            mr.reflectionProbeUsage = serializedSource.reflectionProbeUsage;
        }
        
        [System.Serializable]
        public struct SerializedMeshRenderer
        {
            public int[] materials;
            public ShadowCastingMode shadowCastingMode;
            public bool receiveShadows;
            public LightProbeUsage lightProbeUsage;
            public ReflectionProbeUsage reflectionProbeUsage;

            public SerializedMeshRenderer(MeshRenderer source)
            {
                materials = new int[source.sharedMaterials.Length];
                for (var i = 0; i < source.sharedMaterials.Length; i++)
                {
                    materials[i] = source.sharedMaterials[i].GetInstanceID();
                }

                shadowCastingMode = source.shadowCastingMode;
                receiveShadows = source.receiveShadows;
                lightProbeUsage = source.lightProbeUsage;
                reflectionProbeUsage = source.reflectionProbeUsage;
            }
        }
    }
}
