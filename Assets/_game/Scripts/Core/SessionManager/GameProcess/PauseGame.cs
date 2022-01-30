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
        public event Action OnPause;
        public event Action OnResume;

        public bool IsPause { get; private set; }

        public Task LoadStart()
        {
            KeysControl.Instance.Hot.SetPause += UpdatePause;
            return Task.CompletedTask;
        }


        private void UpdatePause()
        {
            if (!IsPause)
            {
                IsPause = true;
                OnPause?.Invoke();
            }
            else
            {
                IsPause = false;
                OnResume?.Invoke();
            }
        }

        public void Pause()
        {
            if (!IsPause)
            {
                IsPause = true;
                OnPause?.Invoke();
            }
        }

        public void Resume()
        {
            if (IsPause)
            {
                IsPause = false;
                OnResume?.Invoke();
            }
        }

    }
}
