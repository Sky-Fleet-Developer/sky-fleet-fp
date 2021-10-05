using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sirenix.Utilities;
using UnityEngine;

namespace ContentSerializer
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
            public override void GetHash(string prefix, object source, Dictionary<string, string> hash)
            {
                var type = source.GetType();

                if (type.IsArray)
                {
                    HashService.GetArrayHash(prefix, source, hash, Context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    HashService.GetListHash(prefix, source, hash, Context);
                    return;
                }

                if (type.IsEnum || HashService.SimpleTypes.Contains(type))
                {
                    hash.Add(prefix, HashService.Serialize(source));
                }
                else
                {
                    switch (source)
                    {
                        case Component component:
                            hash.Add(prefix, HashService.Serialize(component.GetInstanceID()));
                            break;
                        case UnityEngine.Object obj:
                            hash.Add(prefix, HashService.Serialize(obj.GetInstanceID()));
                            Context.DetectedObjectReport?.Invoke(obj);
                            break;
                        default:
                            HashService.GetNestedHash(prefix, source, hash, Context);
                            break;
                    }
                }
            }

            public override void SetHash(string prefix, Type type, Action<object> setter, ref object source,
                Dictionary<string, string> hash, Dictionary<int, Component> components)
            {
                if (type.IsArray)
                {
                    HashService.SetArrayHash(prefix, type, setter, hash, components, Context);
                    return;
                }

                if (type.InheritsFrom(typeof(IList)))
                {
                    HashService.SetListHash(prefix, type, setter, hash, components, Context);
                    return;
                }

                if (type.IsEnum || HashService.SimpleTypes.Contains(type))
                {
                    if (hash.TryGetValue(prefix, out string value))
                    {
                        setter?.Invoke(HashService.Deserialize(value, type));
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
                        var id = (int) HashService.Deserialize(value, typeof(int));
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
                        var id = (int) HashService.Deserialize(value, typeof(int));
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
                    HashService.SetNestedHash(prefix, ref obj, hash, components, Context);
                    setter?.Invoke(obj);
                }
            }
        }
    }
}