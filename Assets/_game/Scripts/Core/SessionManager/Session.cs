using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Character;
using Core.Data.GameSettings;
using Core.Explorer.Content;
using Core.SessionManager.SaveService;
using Core.Utilities;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SessionManager
{
    [DontDestroyOnLoad]
    public class Session : Singleton<Session>
    {
        public SessionSettings Settings => settings;
        public ControlSettings Control => control;

        public FirstPersonController Player { get; private set; }

        private bool _isInitialized = false;

        [ShowInInspector] private SessionSettings settings = new SessionSettings();
        [ShowInInspector] private ControlSettings control = ControlSettings.GetDefaultSetting();

        [ShowInInspector] private SaveLoad saveLoad = new SaveLoad();

        [ShowInInspector] private SaveLoadUtility saveLoadUtility = new SaveLoadUtility();

        protected override void Setup()
        {
            LoadSettings();
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
        }

        private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != 0)
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            LoadControl();
            SpawnPerson.OnPlayerWasLoaded.Subscribe(InitPlayer);
        }
        
        private void InitPlayer()
        {
            if (Player == null)
            {
                if (!TryFindPlayer())
                {
                    Player = SpawnPerson.Instance.Player;
                }
            }
        }

        private bool TryFindPlayer()
        {
            Player = FindObjectOfType<FirstPersonController>();
            return Player;
        }
        
        private void LoadControl()
        {
            try
            {
                GameSettingsFileManager.LoadSetting(control, PathStorage.GetPathToSettingFile());
                Debug.Log("Saved settings were loaded.");
            }
            catch(System.Exception e)
            {
                Debug.LogError("Error when loading control: " + e);
                return;
            }
        }
        
        public void SaveControlSetting()
        {
            try
            {
                GameSettingsFileManager.SaveSetting(control, PathStorage.GetPathToSettingFile());
            }
            catch(System.Exception e)
            {
                Debug.LogError(e);
                return;
            }
            Debug.Log("The settings were saved successfully.");
        }

        [Button]
        public void SaveWithName(string name)
        {
            saveLoadUtility.SaveWithName(name);
        }

        public async void Load(string filePath, System.Action onComplete)
        {
            await LoadAndComplete(filePath, onComplete);
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
            return _isInitialized;
        }

        public LinkedList<Mod> GetMods()
        {
            return settings.mods;
        }

        public void SetSettings(SessionSettings newSettings)
        {
            if (SceneManager.GetActiveScene().buildIndex == (int)SceneLoader.TypeScene.Menu)
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
            _isInitialized = true;
            Time.timeScale = 1;
        }

        public void Clear()
        {
            _isInitialized = false;
            settings.Clear();
        }
    }
}