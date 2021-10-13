using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer
{
    public class Deserializer : ISerializationContext
    {
        public Action<Object> DetectedObjectReport => throw new NotImplementedException();
        public Action<string> AddTag
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Func<int, Task<Object>> GetObject { get; set; }
        public SerializerBehaviour Behaviour { get; }
        public string ModFolderPath { get; }
        public bool IsCurrentlyBuilded { get; set; }

        public Assembly[] AvailableAssemblies { get; }

        public Type GetTypeByName(string name)
        {
            try
            {
                foreach (Assembly assembly in AvailableAssemblies)
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.FullName == name) return type;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }
        
        public Deserializer(SerializerBehaviour behaviour, string modFolderPath, Assembly[] availableAssemblies)
        {
            AvailableAssemblies = availableAssemblies;
            ModFolderPath = modFolderPath;
            Behaviour = behaviour;
            Behaviour.context = this;
        }
    }
}