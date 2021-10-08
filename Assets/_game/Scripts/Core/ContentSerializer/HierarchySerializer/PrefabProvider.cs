using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;

namespace Core.ContentSerializer.HierarchySerializer
{
    public static class PrefabProvider
    {
        /*[MenuItem("Mod/Build AssetBundles")]
static void GetCurrentSelectionJson()
{
    if (Selection.activeGameObject)
    {
        var serializer = new Serializer(new SerializerBehaviour());
        var bundle = new Serializer.Bundle(Selection.activeGameObject);
        File.WriteAllText("Assets/Json.txt", JsonConvert.SerializeObject(bundle));
    }
}*/

        public static Serializer GetSerializer()
        {
            return new Serializer(new PrefabBehaviour());
        }
        
        public static Deserializer GetDeserializer(string modFolderName, Assembly[] availableAssemblies)
        {
            return new Deserializer(new PrefabBehaviour(), modFolderName, availableAssemblies);
        }

        public class PrefabBehaviour : SerializerBehaviour
        {
            public override void GetCache(string prefix, object source, Dictionary<string, string> hash)
            {
                var type = source.GetType();

                if (type.IsArray)
                {
                    CacheService.GetArrayCache(prefix, source, hash, Context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    CacheService.GetListCache(prefix, source, hash, Context);
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
                            Context.DetectedObjectReport?.Invoke(obj);
                            break;
                        default:
                            CacheService.GetNestedCache(prefix, source, hash, Context);
                            break;
                    }
                }
            }

            public override async Task SetCache(string prefix, Type type, Action<object> setter, object source,
                Dictionary<string, string> hash, Dictionary<int, Component> components)
            {
                if (type.IsArray)
                {
                    await CacheService.SetArrayCache(prefix, type, setter, hash, components, Context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    await CacheService.SetListCache(prefix, type, setter, hash, components, Context);
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
                        Debug.LogError("Has no hash \"" + prefix + "\"");
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
                            Debug.LogError("Has no component with id \"" + id + "\"");
                        }
                    }
                    else
                    {
                        Debug.LogError("Has no hash \"" + prefix + "\"");
                    }
                }
                else if (type.InheritsFrom(typeof(UnityEngine.Object)))
                {
                    if (hash.TryGetValue(prefix, out string value))
                    {
                        var id = (int) CacheService.Deserialize(value, typeof(int));
                        var obj = Context.GetObject(id);
                        if (obj != null)
                        {
                            setter?.Invoke(obj);
                        }
                        else
                        {
                            Debug.LogError("Has no component with id \"" + id + "\"");
                        }
                    }
                    else
                    {
                        Debug.LogError("Has no hash \"" + prefix + "\"");
                    }
                }
                else
                {
                    var obj = Activator.CreateInstance(type);
                    CacheService.SetNestedCache(prefix, obj, hash, components, Context);
                    setter?.Invoke(obj);
                }
            }
        }
    }
}