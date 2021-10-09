using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.ContentSerializer.CustomSerializers
{
    public class MaterialSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            var mat = (Material) source;
            if (idx == 1)
            {
                return mat.shader.name;
            }
            var serializedSource = new SerializedMaterial(mat, context);
            return JsonConvert.SerializeObject(serializedSource, new ColorConverter(), new VectorConverter());
        }

        public int GetStringsCount() => 2;

        public async Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            Material mr = (Material) source;
            var hashValue = cache[prefix];
            var serializedSource = JsonConvert.DeserializeObject<SerializedMaterial>(hashValue);
            
            await serializedSource.ApplyToMaterial(mr, context);
        }

        [System.Serializable]
        public struct SerializedMaterial
        {
            public ShaderProperty[] properties;
            public SerializedMaterial(Material source, ISerializationContext context)
            {
                int count = source.shader.GetPropertyCount();
                properties = new ShaderProperty[count];
                for (int i = 0; i < count; i++)
                {
                    string name = source.shader.GetPropertyName(i);
                    int id = source.shader.GetPropertyNameId(i);
                    ShaderPropertyType type = source.shader.GetPropertyType(i);
                    properties[i] = new ShaderProperty {name = name, type = type};
                    switch (type)
                    {
                        case ShaderPropertyType.Color:
                            properties[i].colorValue = source.GetColor(id);
                            break;
                        case ShaderPropertyType.Vector:
                            properties[i].vectorValue = source.GetVector(id);
                            break;
                        case ShaderPropertyType.Float:
                            properties[i].floatValue = source.GetFloat(id);
                            break;
                        case ShaderPropertyType.Range:
                            properties[i].floatValue = source.GetFloat(id);
                            break;
                        case ShaderPropertyType.Texture:
                            Texture tex = source.GetTexture(id);
                            if (tex != null)
                            {
                                context.DetectedObjectReport(tex);
                                properties[i].texValue = tex.GetInstanceID();
                            }
                            else
                            {
                                properties[i].texValue = -1;
                            }
                            break;
                        default:
                            throw new System.ArgumentOutOfRangeException();
                    }
                }
            }

            public async Task ApplyToMaterial(Material material, ISerializationContext context)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    switch (property.type)
                    {
                        case ShaderPropertyType.Color:
                            material.SetColor(property.name, property.colorValue);
                            break;
                        case ShaderPropertyType.Vector:
                            material.SetVector(property.name, property.vectorValue);
                            break;
                        case ShaderPropertyType.Float:
                            material.SetFloat(property.name, property.floatValue);
                            break;
                        case ShaderPropertyType.Range:
                            material.SetFloat(property.name, property.floatValue);
                            break;
                        case ShaderPropertyType.Texture:
                            if(property.texValue == -1) break;
                            Texture tex = (Texture)await context.GetObject(property.texValue);
                            if (tex != null)
                            {
                                material.SetTexture(property.name, tex);
                            }

                            break;
                        default:
                            throw new System.ArgumentOutOfRangeException();
                    }
                }
            }

            [System.Serializable]
            public struct ShaderProperty
            {
                public ShaderPropertyType type;
                public string name;
                public Color colorValue;
                public float floatValue;
                public Vector4 vectorValue;
                public int texValue;
            }
        }
    }
}
