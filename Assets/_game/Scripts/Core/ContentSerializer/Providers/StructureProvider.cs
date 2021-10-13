using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer.CustomSerializers;
using Core.Structure.Rigging;
using Sirenix.Utilities;
using UnityEngine;

namespace Core.ContentSerializer.Providers
{
    public class StructureProvider
    {
        public static Serializer GetSerializer()
        {
            return new Serializer(new StructureBehaviour());
        }

        public static Deserializer GetDeserializer(Assembly[] availableAssemblies)
        {
            return new Deserializer(new StructureBehaviour(), string.Empty, availableAssemblies);
        }

        public class StructureBehaviour : SerializerBehaviour
        {
            public override async Task SetNestedCache(string prefix, object source, Dictionary<string, string> cache,
                Dictionary<int, Component> components)
            {
                Type type = source.GetType();

                if (FindCustomSerializer(type, out ICustomSerializer serializer))
                {
                    await serializer.Deserialize(prefix, source, cache, context);
                }
            }

            public override void GetNestedCache(string prefix, object source, Dictionary<string, string> cache)
            {
                Type type = source.GetType();

                if (FindCustomSerializer(type, out ICustomSerializer serializer))
                {
                    for (int i = 0; i < serializer.GetStringsCount(); i++)
                    {
                        string postfix = i == 0 ? string.Empty : $"_{i}";
                        cache.Add(prefix + postfix, serializer.Serialize(source, context, i));
                    }
                }
            }
        }
        
        private static bool FindCustomSerializer(System.Type t, out ICustomSerializer value)
        {
            if (CustomSerializer.TryGetValue(t, out ICustomSerializer val))
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

        private static readonly Dictionary<System.Type, ICustomSerializer> CustomSerializer =
            new Dictionary<System.Type, ICustomSerializer>
            {
                {typeof(Transform), new TransformSerializer()},
                {typeof(IBlock), new IBlockSerializer()},
                {typeof(Rigidbody), new RigidbodySerializer()},
            };
    }
}