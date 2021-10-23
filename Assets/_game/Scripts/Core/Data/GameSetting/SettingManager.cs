using Core.Boot_strapper;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace Core.GameSetting
{
    [DontDestroyOnLoad]
    public class SettingManager : Singleton<SettingManager>, ILoadAtStart
    {
        public Setting GetSetting() => setting;
        public ControlSetting GetControlSetting() => setting.Control;

        private Setting setting;

        public Task Load()
        {
            setting = new Setting();
            if(!GameSettingFileManager.LoadSetting(setting, PathStorage.GetPathToSettingFile()))
            {
                Debug.Log("Saved settings were not loaded.");
            }
            else
            {
                Debug.Log("Saved settings were loaded.");
            }
            return Task.CompletedTask;
        }

        public void SaveSetting()
        {
            if (GameSettingFileManager.SaveSetting(setting, PathStorage.GetPathToSettingFile()))
            {
                Debug.Log("The settings were saved successfully.");
            }
        }
    }
}