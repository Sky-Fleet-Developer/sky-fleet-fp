using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Explorer.Content
{
    /// <summary>
    /// Класс отвечает за чтение папки Mods и поиск директорий
    /// </summary>
    public class ModReader : Singleton<ModReader>, ILoadAtStart
    {
        public int CountMods => mods.Count;

        [ShowInInspector] private List<Mod> mods = new List<Mod>();

        private static event System.Action<List<Mod>> onModsLoaded;
        [System.NonSerialized, ShowInInspector, ReadOnly] public static bool isModsLoaded = false;

        public Task Load()
        {
            LinkedList<string> directories = FindAllMods(GetPathDirectoryMods());
            foreach (string name in directories)
            {
                Debug.Log("Find mod: " + name);
            }
            
            GenerateMods(directories, new ModLoader());

            onModsLoaded?.Invoke(mods);
            isModsLoaded = true;
            onModsLoaded = null;
            
            Bootstrapper.OnLoadComplete.Subscribe(LaunchExes);
            
            return Task.CompletedTask;
        }

        private void LaunchExes()
        {
            foreach (Mod mod in mods)
            {
                mod.LaunchExeIsExist();
            }
        }


        /// <summary>
        /// call action immediately if mods already loaded. Write callback to action "onModsLoaded" if mods not loaded yet
        /// </summary>
        /// <param name="callback">action to call when mods will be loaded</param>
        public static void OnModsLoaded(System.Action<List<Mod>> callback)
        {
            if (isModsLoaded) callback?.Invoke(Instance.mods);
            else onModsLoaded += callback;
        }
        

        public Mod GetMod(int index)
        {
            return mods[index];
        }

        public List<Mod> GetMods()
        {
            if (isModsLoaded)
            {
                return mods;
            }
            return null;
        }

        private void GenerateMods(LinkedList<string> directories, ModLoader loader)
        {
            foreach (string directory in directories)
            {
                LoadMod(directory, loader);
            }
        }

        private void LoadMod(string modDirectory, ModLoader loader)
        {
            Mod mod = loader.Read(modDirectory);
            if (mod != null)
            {
                mods.Add(mod);
                mod.CreateExeIsExist();
            }
        }

        private string GetPathDirectoryMods()
        {
            string pathU = Application.dataPath;
            DirectoryInfo infoPath = Directory.GetParent(pathU);
            return infoPath.FullName + "/Mods/";
        }

        private void CorrectDirectory(string path)
        {
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private LinkedList<string> FindAllMods(string rootPath)
        {
            LinkedList<string> list = new LinkedList<string>();
            CorrectDirectory(rootPath);
            string[] directoryMods = Directory.GetDirectories(rootPath);
            for (int i = 0; i < directoryMods.Length; i++)
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