using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer.AssetCreators;
using Core.ContentSerializer.CustumSerializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer
{
    public static class CacheService
    {
        public static void SetNestedCache(string prefix, ref object source, Dictionary<string, string> hash,
            Dictionary<int, Component> components, ISerializationContext context)
        {
            var type = source.GetType();
            
            if (CustomSerializer.TryGetValue(type, out var serializer))
            {
                serializer.Deserialize(prefix, source, hash, context);
                return;
            }
            
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var obj = source;
            for (var index = 0; index < fields.Length; index++)
            {
                var fieldInfo = fields[index];
                if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null ||
                    fieldInfo.IsNotSerialized) continue;

                var value = fieldInfo.GetValue(source);
                context.Behaviour.SetCache(prefix + "." + fieldInfo.Name, fieldInfo.FieldType, o =>
                        fieldInfo.SetValue(obj, o),
                    ref value, hash, components);
                source = obj;
            }

            var properties = type.GetProperties();
            for (var index = 0; index < properties.Length; index++)
            {
                var propertyInfo = properties[index];
                if (CacheService.CanSerializeProperty(type, propertyInfo))
                {
                    var value = propertyInfo.GetValue(source);
                    context.Behaviour.SetCache(prefix + "." + propertyInfo.Name, propertyInfo.PropertyType,
                        o => propertyInfo.SetValue(obj, o), ref value, hash, components);
                    source = obj;
                }
            }
        }

        public static void SetArrayCache(string prefix, Type type, Action<object> setter,
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
                context.Behaviour.SetCache($"{prefix}[{i}]", elementType, o => v = o, ref obj, hash, components);
                array[i] = v;
            }

            setter?.Invoke(array);
        }

        public static void SetListCache(string prefix, Type type, Action<object> setter,
            Dictionary<string, string> hash,
            Dictionary<int, Component> components, ISerializationContext context)
        {
            int count = int.Parse(hash[prefix]);
            var list = Activator.CreateInstance(type) as IList;
            var elementType = type.GetGenericArguments().Single();
            object obj = list;

            for (int i = 0; i < count; i++)
            {
                context.Behaviour.SetCache($"{prefix}[{i}]", elementType, o => list.Add(o), ref obj, hash, components);
            }

            setter?.Invoke(list);
        }

        public static void GetNestedCache(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            var type = source.GetType();
            
            if (CustomSerializer.TryGetValue(type, out var serializer))
            {
                for (int i = 0; i < serializer.GetStringsCount(); i++)
                {
                    string postfix = i == 0 ? string.Empty : $"_{i}";
                    hash.Add(prefix + postfix, serializer.Serialize(source, context, i));
                }
                return;
            }
            
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var index = 0; index < fields.Length; index++)
            {
                var fieldInfo = fields[index];
                if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null ||
                    fieldInfo.IsNotSerialized) continue;
                var value = fieldInfo.GetValue(source);
                if (value == null) continue;
                context.Behaviour.GetCache(prefix + "." + fieldInfo.Name, value, hash);
            }

            var properties = type.GetProperties();
            for (var index = 0; index < properties.Length; index++)
            {
                var propertyInfo = properties[index];
                if (CanSerializeProperty(type, propertyInfo))
                {
                    try
                    {
                        var value = propertyInfo.GetValue(source);
                        if (value == null) continue;
                        context.Behaviour.GetCache(prefix + "." + propertyInfo.Name, value, hash);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public static void GetArrayCache(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            if (!(source is System.Array arr)) return;
            hash.Add(prefix, arr.Length.ToString());
            for (int i = 0; i < arr.Length; i++)
            {
                context.Behaviour.GetCache($"{prefix}[{i}]", arr.GetValue(i), hash);
            }
        }

        public static void GetListCache(string prefix, object source, Dictionary<string, string> hash,
            ISerializationContext context)
        {
            var list = source as IList;
            hash.Add(prefix, list.Count.ToString());
            for (int i = 0; i < list.Count; i++)
            {
                context.Behaviour.GetCache($"{prefix}[{i}]", list[i], hash);
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

        public static readonly Dictionary<Type, ICustomSerializer> CustomSerializer =
            new Dictionary<Type, ICustomSerializer>
            {
                {typeof(MeshFilter), new MeshFilterSerializer()},
                {typeof(Mesh), new MeshSerializer()},
                {typeof(MeshRenderer), new MeshRendererSerializer()},
                {typeof(Material), new MaterialSerializer()},
                {typeof(Texture2D), new Texture2DSerializer()},
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
        Func<int, UnityEngine.Object> GetObject { get; }
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
        Task Deserialize(string prefix, object source, Dictionary<string, string> hash, ISerializationContext context);
    }

    public interface IAssetCreator
    {
        Task<Object> CreateInstance(string prefix, Dictionary<string, string> hash,
            ISerializationContext context);
    }

    public abstract class SerializerBehaviour
    {
        public ISerializationContext Context;
        public abstract void GetCache(string prefix, object source, Dictionary<string, string> hash);

        public abstract void SetCache(string prefix, Type type, Action<object> setter, ref object source,
            Dictionary<string, string> hash, Dictionary<int, Component> components);
    }

   
}