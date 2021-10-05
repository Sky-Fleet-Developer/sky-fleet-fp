using System;
using System.Reflection;
using Object = UnityEngine.Object;

namespace ContentSerializer
{
    public class Deserializer : ISerializationContext
    {
        public Action<Object> DetectedObjectReport => throw new NotImplementedException();
        public Func<int, Object> GetObject { get; set; }
        public SerializerBehaviour Behaviour { get; }
        public string ModFolderPath { get; }

        public Assembly[] AvailableAssemblies { get; }

        public Type GetTypeByName(string name)
        {
            foreach (var assembly in AvailableAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == name) return type;
                }
            }

            return null;
        }
        
        public Deserializer(SerializerBehaviour behaviour, string modFolderPath, Assembly[] availableAssemblies)
        {
            AvailableAssemblies = availableAssemblies;
            ModFolderPath = modFolderPath;
            Behaviour = behaviour;
            Behaviour.Context = this;
        }
    }
}