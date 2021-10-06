using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ContentSerializer;
using UnityEngine;

namespace Core.Explorer.Content
{
    public class Mod
    {
        private SerializationModule module;
        private Assembly assembly;
        public string name;
        
        public Mod(string modFolderPath, SerializationModule module, Assembly assembly)
        {
            this.module = module;
            this.assembly = assembly;
            module.ModFolderPath = modFolderPath;
            var pathSplit = modFolderPath.Split(new[] {'/', '\\'});
            name = pathSplit[pathSplit.Length - 1];

            //DeserializeAll(); //проверка работоспособности
        }

        public async void DeserializeAll()
        {
            List<Assembly> assemblies = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
            assemblies.Add(assembly);
            await module.DeserializeAll(assemblies.ToArray());
        }
    }
}