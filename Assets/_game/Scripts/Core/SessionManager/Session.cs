using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Explorer.Content;
using Core.SessionManager.SaveService;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

namespace Core.SessionManager
{
    [DontDestroyOnLoad]
    public class Session : Singleton<Session>
    {
        public SessionSettings Settings => settings;
        
        private bool isInitialized = false;

        [ShowInInspector] private SessionSettings settings;

        [ShowInInspector] private SaveLoad saveLoad = new SaveLoad();

        [Button]
        public void Save()
        {
            saveLoad.Save();
        }
        
        [Button]
        public Task Load(string fileName)
        {
            return saveLoad.Load(fileName);
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }

        public LinkedList<Mod> GetMods()
        {
            return settings.mods;
        }

        public void SetSettings(SessionSettings newSettings)
        {
            if(SceneManager.sceneCountInBuildSettings == (int)SceneLoader.TypeScene.Menu)
            {
                settings = newSettings;
            }
        }

        public void BeginInit()
        {
            Clear();
        }

        public void EndInit()
        {
            isInitialized = true;
        }
        
        public void Clear()
        {
            isInitialized = false;
            settings.Clear();
        }
    }
}