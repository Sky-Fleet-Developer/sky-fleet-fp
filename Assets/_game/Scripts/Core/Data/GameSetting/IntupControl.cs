using Core.Boot_strapper;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace Core.GameSetting
{
    [DontDestroyOnLoad]
    public class IntupControl : Singleton<SettingManager>, ILoadAtStart
    {
        public Task Load()
        {
            return Task.CompletedTask;
        }
    }
}