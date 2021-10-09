using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer.AssetCreators;
using Core.ContentSerializer.CustomSerializers;
using Core.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer
{
    public static class CacheService
    {
        public static void GetCache(string prefix, object source, Dictionary<string, string> hash, ISerializationContext context)
            {
                var type = source.GetType();

                if (type.IsArray)
                {
                    CacheService.GetArrayCache(prefix, source, hash, context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    CacheService.GetListCache(prefix, source, hash, context);
                    return;
                }

                if (type.IsEnum || CacheService.SimpleTypes.Contains(type))
                {
                    hash.Add(prefix, CacheService.Serialize(source));
                }
                else
                {
                    switch (source)
                    {
                        case Component component:
                            hash.Add(prefix, CacheService.Serialize(component.GetInstanceID()));
                            break;
                        case UnityEngine.Object obj:
                            hash.Add(prefix, CacheService.Serialize(obj.GetInstanceID()));
                            context.DetectedObjectReport?.Invoke(obj);
                            break;
                        default:
                            context.Behaviour.GetNestedCache(prefix, source, hash);
                            break;
                    }
                }
            }

        public static async Task SetCache(string prefix, Type type, Action<object> setter, object source,
                Dictionary<string, string> hash, Dictionary<int, Component> components, ISerializationContext context)
            {
                if (type.IsArray)
                {
                    await CacheService.SetArrayCache(prefix, type, setter, hash, components, context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    await CacheService.SetListCache(prefix, type, setter, hash, components, context);
                    return;
                }

                if (type.IsEnum || CacheService.SimpleTypes.Contains(type))
                {
                    if (hash.TryGetValue(prefix, out string value))
                    {
                        setter?.Invoke(CacheService.Deserialize(value, type));
                    }
                    else
                    {
                        Debug.LogWarning("Has no hash \"" + prefix + "\"");
                    }
                }
                else if (type.InheritsFrom(typeof(Component)))
                {
                    if (hash.TryGetValue(prefix, out string value))
                    {
                        var id = (int) CacheService.Deserialize(value, typeof(int));
                        if (components.TryGetValue(id, out Component component))
                        {
                            setter?.Invoke(component);
                        }
                        else
                        {
                            Debug.LogWarning("Has no component with id \"" + id + "\"");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Has no hash \"" + prefix + "\"");
                    }
                }
                else if (type.InheritsFrom(typeof(UnityEngine.Object)))
                {
                    if (hash.TryGetValue(prefix, out string value))
                    {
                        var id = (int) CacheService.Deserialize(value, typeof(int));
                        var obj = context.GetObject(id);
                        if (obj != null)
                        {
                            setter?.Invoke(obj);
                        }
                        else
                        {
                            Debug.LogWarning("Has no component with id \"" + id + "\"");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Has no hash \"" + prefix + "\"");
                    }
                }
                else
                {
                    var obj = Activator.CreateInstance(type);
                    await context.Behaviour.SetNestedCache(prefix, obj, hash, components);
                    setter?.Invoke(obj);
                }
            }
        
        

        public static async Task SetArrayCache(string prefix, Type type, Action<object> setter,
            Dictionary<string, string> hash,
            Dictionary<int, Component> components, ISerializationContext context)
        {
            int count = int.Parse(hash[prefix]);
            var array = Activator.CreateInstance(type, count) as object[];
            var elementType = type.GetElementType();
            object obj = array;

            for (int i = 0; i < count; i++)
            {
                var v = array[i];
                await SetCache($"{prefix}[{i}]", elementType, o => v = o, obj, hash, components, context);
                array[i] = v;
            }

            setter?.Invoke(array);
        }

        public static async Task SetListCache(string prefix, Type type, Action<object> setter,
            Dictionary<string, string> hash,
            Dictionary<int, Component> components, ISerializationContext context)
        {
            int count = int.Parse(hash[prefix]);
            var list = Activator.CreateInstance(type) as IList;
            var elementType = type.GetGenericArguments().Single();
            object obj = list;

            for (int i = 0; i < count; i++)
            {
                await SetCache($"{prefix}[{i}]", elementType, o => list.Add(o), obj, hash, components, context);
            }

            setter?.Invoke(list);
        }
        

        public static void GetArrayCache(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            if (!(source is System.Array arr)) return;
            hash.Add(prefix, arr.Length.ToString());
            for (int i = 0; i < arr.Length; i++)
            {
                GetCache($"{prefix}[{i}]", arr.GetValue(i), hash, context);
            }
        }

        public static void GetListCache(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            var list = source as IList;
            hash.Add(prefix, list.Count.ToString());
            for (int i = 0; i < list.Count; i++)
            {
                GetCache($"{prefix}[{i}]", list[i], hash, context);
            }
        }

        public static string Serialize(object source)
        {
            if (CustomConvertor.TryGetValue(source.GetType(), out JsonConverter converter))
            {
                return JsonConvert.SerializeObject(source, converter);
            }

            return JsonConvert.SerializeObject(source);
        }

        public static object Deserialize(string value, Type objectType)
        {
            return JsonConvert.DeserializeObject(value, objectType);
        }

        public static bool CanSerializeProperty(Type sourceType, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
            {
                return false;
            }

            if (!SimpleTypes.Contains(propertyInfo.PropertyType))
            {
                return false;
            }

            if (propertyInfo.PropertyType == sourceType)
            {
                return false;
            }

            foreach (var (type, property) in forbiddenProperties)
            {
                if (type == sourceType && propertyInfo.Name == property)
                {
                    return false;
                }
            }

            return true;
        }

        public static readonly Type[] SimpleTypes =
        {
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(short),
            typeof(long),
            typeof(int),
            typeof(bool),
            typeof(string),
            typeof(Vector3),
            typeof(Vector3Int),
            typeof(Vector2),
            typeof(Vector2Int),
            typeof(Quaternion),
            typeof(AnimationCurve),
        };

        public static bool FindCustomSerializer(Type t, out ICustomSerializer value)
        {
            if (CustomSerializer.TryGetValue(t, out var val))
            {
                value = val;
                return true;
            }
            
            foreach (var serializer in CustomSerializer)
            {
                if (t.InheritsFrom(serializer.Key))
                {
                    value = serializer.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public static readonly Dictionary<Type, ICustomSerializer> CustomSerializer =
            new Dictionary<Type, ICustomSerializer>
            {
                {typeof(MeshFilter), new MeshFilterSerializer()},
                {typeof(Mesh), new MeshSerializer()},
                {typeof(MeshRenderer), new MeshRendererSerializer()},
                {typeof(Material), new MaterialSerializer()},
                {typeof(Texture2D), new Texture2DSerializer()},
                {typeof(Port), new PortSerializer()},
                {typeof(Transform), new TransformSerializer()}
            };

        public static readonly Dictionary<Type, IAssetCreator> AssetCreators =
            new Dictionary<Type, IAssetCreator>
            {
                {typeof(Material), new MaterialCreator()},
                {typeof(Texture2D), new Texture2DCreator()}
            };

        public static readonly Dictionary<Type, JsonConverter> CustomConvertor =
            new Dictionary<Type, JsonConverter>
            {
                {typeof(Vector3), new VectorConverter()},
                {typeof(Vector2), new VectorConverter()},
                {typeof(Quaternion), new QuaternionConverter()},
            };

        public static readonly List<(Type type, string property)> forbiddenProperties =
            new List<(Type type, string property)>()
            {
                (typeof(Vector3), "normalized"),
                (typeof(Vector2), "normalized"),
                (typeof(Quaternion), "normalized"),
            };
    }

    public interface ISerializationContext
    {
        Action<UnityEngine.Object> DetectedObjectReport { get; }
        Action<string> AddTag { get; set; }
        Func<int, Task<Object>> GetObject { get; }
        Assembly[] AvailableAssemblies { get; }
        Type GetTypeByName(string name);
        SerializerBehaviour Behaviour { get; }
        string ModFolderPath { get; }
        bool IsCurrentlyBuilded { get; }
    }
    
    public interface ICustomSerializer
    {
        string Serialize(object source, ISerializationContext context, int idx);
        int GetStringsCount();
        Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context);
    }

    public interface IAssetCreator
    {
        Task<Object> CreateInstance(string prefix, Dictionary<string, string> cache,
            ISerializationContext context);
    }

    public abstract class SerializerBehaviour
    {
        public ISerializationContext context;

        public abstract void GetNestedCache(string prefix, object source, Dictionary<string, string> cache);

        public abstract Task SetNestedCache(string prefix, object source, Dictionary<string, string> cache,
            Dictionary<int, Component> components);
    }

   
}