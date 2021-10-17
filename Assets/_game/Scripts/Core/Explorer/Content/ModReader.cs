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

        public async Task Load()
        {
            LinkedList<string> modsD = GetListMods(GetPathDirectoryMods());
            foreach (string name in modsD)
            {
                Debug.Log("Find mod: " + name);
            }
            
            List<Task> loadTasks = GenerateMods(modsD, new ModLoader());

            await Task.WhenAll(loadTasks);
            onModsLoaded?.Invoke(mods);
            isModsLoaded = true;
            onModsLoaded = null;
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

        private List<Task> GenerateMods(LinkedList<string> directorys, ModLoader loader)
        {
            List<Task> tasks = new List<Task>();
            foreach (string directory in directorys)
            {
                Task task = LoadMod(directory, loader);
                tasks.Add(task);
            }

            return tasks;
        }

        private async Task LoadMod(string modDirectory, ModLoader loader)
        {
            Mod mod = await loader.Read(modDirectory);
            if (mod != null)
            {
                mods.Add(mod);
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