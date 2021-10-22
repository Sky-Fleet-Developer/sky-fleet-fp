using System;
using System.Threading.Tasks;
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
            return Task.CompletedTask;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPause)
                {
                    IsPause = false;
                    Time.timeScale = 1;
                    PauseOff?.Invoke();
                }
                else
                {
                    IsPause = true;
                    Time.timeScale = 0;
                    PauseOn?.Invoke();
                }
            }
        }

        public void SetOnPause()
        {
            if (IsPause)
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
