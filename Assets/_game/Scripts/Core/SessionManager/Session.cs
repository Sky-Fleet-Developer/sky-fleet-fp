using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Explorer.Content;
using Core.SessionManager.SaveService;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SessionManager
{
    [DontDestroyOnLoad]
    public class Session : Singleton<Session>
    {
        public SessionSettings Settings => settings;
        
        private bool isInitialized = false;

        [ShowInInspector] private SessionSettings settings = new SessionSettings();

        [ShowInInspector] private SaveLoad saveLoad = new SaveLoad();

        [ShowInInspector] private SaveLoadUtility saveLoadUtility = new SaveLoadUtility();

        [Button]
        public void SaveWithName(string name)
        {
            saveLoadUtility.SaveWithName(name);
        }

        public void Load(string filePath, System.Action onComplete)
        {
            var task = LoadAndComplete(filePath, onComplete);
        }

        private async Task LoadAndComplete(string filePath, System.Action onComplete)
        {
            await Load(filePath);
            onComplete?.Invoke();
        }
        
        [Button]
        public Task Load(string filePath)
        {
            return saveLoad.Load(filePath);
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
            if(SceneManager.GetActiveScene().buildIndex == (int)SceneLoader.TypeScene.Menu)
            {
                settings = newSettings;
            }
        }

        public void BeginInit()
        {
            Clear();
            Time.timeScale = 0;
        }

        public void EndInit()
        {
            isInitialized = true;
            Time.timeScale = 1;
        }
        
        public void Clear()
        {
            isInitialized = false;
            settings.Clear();
        }
    }
}