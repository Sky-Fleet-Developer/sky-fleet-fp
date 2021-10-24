using System;
using System.Threading.Tasks;
using Core.GameSetting;
using Core.Boot_strapper;
using Core.Utilities;
using UnityEngine;

namespace Core.SessionManager.GameProcess
{
    public class PauseGame : Singleton<PauseGame>, ILoadAtStart
    {
        public event Action PauseOn;
        public event Action PauseOff;

        public bool IsPause { get; private set; }

        public Task Load()
        {
            HotKeys.Instance.SetPause += UpdatePause;
            return Task.CompletedTask;
        }


        private void UpdatePause()
        {
            if (!IsPause)
            {
                IsPause = true;
                PauseOn();
            }
            else
            {
                IsPause = false;
                PauseOff();
            }
        }

        public void SetOnPause()
        {
            if (!IsPause)
            {
                IsPause = true;
                PauseOn();
            }
        }

        public void SetOffPause()
        {
            if (IsPause)
            {
                IsPause = false;
                PauseOff();
            }
        }

    }
}
