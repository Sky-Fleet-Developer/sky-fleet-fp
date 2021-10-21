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

            return Task.CompletedTask;
        }
    }
}