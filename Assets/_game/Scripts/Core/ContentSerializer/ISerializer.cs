using System;
using System.Collections.Generic;
using System.IO;
using Core.Utilities;

namespace Core.ContentSerializer
{
    public class Serializers
    {
        private static Serializers _instance = new ();
        private Dictionary<Type, ISerializer> _serializersByType = new ();
        private Dictionary<string, ISerializer> _serializersByName = new ();
        
        private Serializers()
        {
        }
        
        public static void Register<T>(ISerializer<T> serializer)
        {
            _instance._serializersByType.Add(typeof(T), serializer);
            string fullName = typeof(T).FullName;
            if (fullName != null)
            {
                _instance._serializersByName.Add(fullName, serializer);
            }
        }
        public static ISerializer GetSerializer(string name)
        {
            if (!_instance._serializersByName.TryGetValue(name, out ISerializer serializer))
            {
                var type = TypeExtensions.GetTypeByName(name);
                if(type == null) return null;
                foreach (var nestedType in type.GetNestedTypes()) 
                {
                    if (nestedType.Name == "Serializer")
                    { 
                        serializer = (ISerializer)Activator.CreateInstance(nestedType);
                        _instance._serializersByType.Add(type, serializer);
                        if (type.FullName != null)
                        {
                            _instance._serializersByName.Add(type.FullName, serializer);
                        }
                        break;
                    }
                }
            }
            return serializer;
        }

        public static ISerializer GetSerializer(Type type)
        {
            if (!_instance._serializersByType.TryGetValue(type, out ISerializer serializer))
            {
                foreach (var nestedType in type.GetNestedTypes()) 
                {
                    if (nestedType.Name == "Serializer")
                    { 
                        serializer = (ISerializer)Activator.CreateInstance(nestedType);
                        _instance._serializersByType.Add(type, serializer);
                        if (type.FullName != null)
                        {
                            _instance._serializersByName.Add(type.FullName, serializer);
                        }
                        break;
                    }
                }
            }
            return serializer;
        }
    }
    public interface ISerializer
    {
        void Serialize(object obj, Stream stream);
        object Deserialize(Stream stream);
        void Populate(Stream stream, ref object obj);
    }

    public interface ISerializer<T> : ISerializer
    {
        void ISerializer.Serialize(object obj, Stream stream)
        {
            Serialize((T)obj, stream);
        }

        object ISerializer.Deserialize(Stream stream)
        {
            return Deserialize(stream);
        }

        void ISerializer.Populate(Stream stream, ref object obj)
        {
            var oT = (T)obj;
            Populate(stream, ref oT);
            obj = oT;
        }
        void Serialize(T obj, Stream stream);
        new T Deserialize(Stream stream);
        void Populate(Stream stream, ref T obj);
    }
}