using System;
using System.Linq;
using System.Collections.Generic;
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

        private LinkedList<Type> classes = new LinkedList<Type>();

        private LinkedList<string> prefabsNames = new LinkedList<string>();

        private LinkedList<string> assetsNames = new LinkedList<string>();

        public Mod(string modFolderPath, SerializationModule module, Assembly assembly)
        {
            this.module = module;
            this.assembly = assembly;
            module.ModFolderPath = modFolderPath;
            var pathSplit = modFolderPath.Split(new[] {'/', '\\'});
            name = pathSplit[pathSplit.Length - 1];


            foreach (Type classT in assembly.GetTypes())
            {
                if(classT.IsClass && classT.IsPublic && classT.IsSubclassOf(typeof(MonoBehaviour)))
                    classes.AddLast(classT);
            }
            foreach (PrefabBundle prefab in module.prefabsHash)
            {
                prefabsNames.AddLast(prefab.name);
            }
            foreach (ContentSerializer.AssetBundle assetsN in module.assetsHash)
            {
                assetsNames.AddLast(assetsN.name);
            }
            //DeserializeAll(); //проверка работоспособности
        }

        public LinkedList<Type> GetClasses()
        {
            return classes;
        }

        public LinkedList<string> GetPrefabsNames()
        {
            return prefabsNames;
        }

        public LinkedList<string> GetAssetsNames()
        {
            return assetsNames;
        }

        public async void DeserializeAll()
        {
            List<Assembly> assemblies = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
            assemblies.Add(assembly);
            await module.DeserializeAll(assemblies.ToArray());
        }
    }
}