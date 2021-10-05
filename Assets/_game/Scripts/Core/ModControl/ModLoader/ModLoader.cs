using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

using Core.Boot_strapper;

namespace Core.Mods
{
    public class ModLoader : MonoBehaviour, ILoadAtStart
    {
        public static ModLoader Singleton { get; private set; }

        public int CountMods => mods.Count;

        private List<Mod> mods = new List<Mod>();

        public Task Load()
        {
            if(Singleton == null)
            {
                Singleton = this;
            }    
            else
            {
                Destroy(gameObject);
            }
            LinkedList<string> modsD = GetListMods(GetPathDirectoryMods());
            foreach(string name in modsD)
            {
                Debug.Log("Find mod: " + name);
            }
            GenerateMods(modsD, new ModReader());


            return Task.CompletedTask;
        }

        public Mod GetMod(int index)
        {
            return mods[index];
        }

        private void GenerateMods(LinkedList<string> directorys, ModReader reader)
        {
            foreach(string modDirectory in directorys)
            {
                Mod mod = reader.Read(modDirectory);
                if(mod != null)
                {
                    mods.Add(mod);
                }
            }
        }

        private string GetPathDirectoryMods()
        {
            string pathU = Application.dataPath;
            DirectoryInfo infoPath = Directory.GetParent(pathU);
            return infoPath.FullName + "/Mods/";
        }

        private LinkedList<string> GetListMods(string pathToMods)
        {
            LinkedList<string> list = new LinkedList<string>();
            string[] directoryMods = Directory.GetDirectories(pathToMods);
            for(int i = 0; i < directoryMods.Length; i++)
            {
                if (File.Exists(directoryMods[i] + "/modDefine.json"))
                {
                    list.AddLast(directoryMods[i]);
                }
            }
            return list;
        }
    }
}