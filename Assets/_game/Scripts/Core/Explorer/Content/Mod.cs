using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Core.ContentSerializer;
using Core.ContentSerializer.HierarchySerializer;
using UnityEngine;
using AssetBundle = Core.ContentSerializer.ResourceSerializer.AssetBundle;

namespace Core.Explorer.Content
{
    [System.Serializable]
    public class Mod
    {
        public SerializationModule module;
        private Assembly assembly;
        public string name;

        private LinkedList<System.Type> classes = new LinkedList<System.Type>();

        private LinkedList<string> prefabsNames = new LinkedList<string>();

        private LinkedList<string> assetsNames = new LinkedList<string>();

        public Assembly[] assemblies;

        public Mod(string modFolderPath, SerializationModule module, Assembly assembly)
        {
            this.module = module;
            this.assembly = assembly;
            module.ModFolderPath = modFolderPath;
            var pathSplit = modFolderPath.Split(new[] {'/', '\\'});
            name = pathSplit[pathSplit.Length - 1];

            var assembliesList = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
            assembliesList.Add(assembly);
            assemblies = assembliesList.ToArray();
            
            foreach (System.Type classT in assembly.GetTypes())
            {
                if(classT.IsClass && classT.IsPublic && classT.IsSubclassOf(typeof(MonoBehaviour)))
                    classes.AddLast(classT);
            }
            foreach (Bundle bundle in module.Cache)
            {
                switch (bundle)
                {
                    case PrefabBundle prefab:
                        prefabsNames.AddLast(prefab.name);
                        break;
                    case AssetBundle asset:
                        assetsNames.AddLast(asset.name);
                        break;
                }
            }
        }

        public LinkedList<System.Type> GetClasses()
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
    }
}