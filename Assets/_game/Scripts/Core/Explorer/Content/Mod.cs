using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using UnityEngine;
using AssetBundle = Core.ContentSerializer.Bundles.AssetBundle;

namespace Core.Explorer.Content
{
    [System.Serializable]
    public class Mod
    {
        public SerializationModule module;
        public Assembly assembly;
        public string name;

        private LinkedList<System.Type> monoTypes = new LinkedList<System.Type>();

        private LinkedList<string> prefabsNames = new LinkedList<string>();

        private LinkedList<string> assetsNames = new LinkedList<string>();

        public Assembly[] AllAssemblies;

        private ModExe exe;

        public Mod(string modFolderPath, SerializationModule module, Assembly assembly)
        {
            this.module = module;
            this.assembly = assembly;
            module.ModFolderPath = modFolderPath;
            string[] pathSplit = modFolderPath.Split(new[] {'/', '\\'});
            name = pathSplit[pathSplit.Length - 1];

            List<Assembly> assembliesList = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
            assembliesList.Add(assembly);
            AllAssemblies = assembliesList.ToArray();
            
            foreach (System.Type classT in assembly.GetTypes())
            {
                if(classT.IsClass && classT.IsPublic && classT.IsSubclassOf(typeof(MonoBehaviour)))
                    monoTypes.AddLast(classT);
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
            return monoTypes;
        }

        public LinkedList<string> GetPrefabsNames()
        {
            return prefabsNames;
        }

        public LinkedList<string> GetAssetsNames()
        {
            return assetsNames;
        }

        public void CreateExeIsExist()
        {
            Type exeT = typeof(ModExe);
            foreach (Type t in assembly.GetTypes())
            {
                if (t.IsSubclassOf(exeT))
                {
                    exe = Activator.CreateInstance(t, this) as ModExe;
                }
            }
        }

        public void LaunchExeIsExist()
        {
            exe?.Main();
        }
    }
}