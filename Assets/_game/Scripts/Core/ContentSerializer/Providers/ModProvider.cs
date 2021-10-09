using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.ContentSerializer.Providers
{
    public static class ModProvider
    {
        public static Serializer GetSerializer()
        {
            return new Serializer(new ModBehaviour());
        }
        
        public static Deserializer GetDeserializer(string modFolderName, Assembly[] availableAssemblies)
        {
            return new Deserializer(new ModBehaviour(), modFolderName, availableAssemblies);
        }

        public class ModBehaviour : SerializerBehaviour
        {
            public override async Task SetNestedCache(string prefix, object source, Dictionary<string, string> cache,
                        Dictionary<int, Component> components)
                    {
                        var type = source.GetType();
                        
                        if (CacheService.FindCustomSerializer(type, out var serializer))
                        {
                            await serializer.Deserialize(prefix, source, cache, context);
                        }
                        
                        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        var obj = source;
                        for (var index = 0; index < fields.Length; index++)
                        {
                            var fieldInfo = fields[index];
                            if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null ||
                                fieldInfo.IsNotSerialized) continue;
            
                            var value = fieldInfo.GetValue(source);
                            await CacheService.SetCache(prefix + "." + fieldInfo.Name, fieldInfo.FieldType, o =>
                                    fieldInfo.SetValue(obj, o),
                                value, cache, components, context);
                            source = obj;
                        }
            
                        var properties = type.GetProperties();
                        for (var index = 0; index < properties.Length; index++)
                        {
                            var propertyInfo = properties[index];
                            if (CacheService.CanSerializeProperty(type, propertyInfo))
                            {
                                var value = propertyInfo.GetValue(source);
                                await CacheService.SetCache(prefix + "." + propertyInfo.Name, propertyInfo.PropertyType,
                                    o => propertyInfo.SetValue(obj, o), value, cache, components, context);
                                source = obj;
                            }
                        }
                    }
            
            public override void GetNestedCache(string prefix, object source, Dictionary<string, string> cache)
                    {
                        var type = source.GetType();
                        
                        if (CacheService.FindCustomSerializer(type, out var serializer))
                        {
                            for (int i = 0; i < serializer.GetStringsCount(); i++)
                            {
                                string postfix = i == 0 ? string.Empty : $"_{i}";
                                cache.Add(prefix + postfix, serializer.Serialize(source, context, i));
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
                            CacheService.GetCache(prefix + "." + fieldInfo.Name, value, cache, context);
                        }
            
                        var properties = type.GetProperties();
                        for (var index = 0; index < properties.Length; index++)
                        {
                            var propertyInfo = properties[index];
                            if (CacheService.CanSerializeProperty(type, propertyInfo))
                            {
                                try
                                {
                                    var value = propertyInfo.GetValue(source);
                                    if (value == null) continue;
                                    CacheService.GetCache(prefix + "." + propertyInfo.Name, value, cache, context);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                    }
        }
    }
}