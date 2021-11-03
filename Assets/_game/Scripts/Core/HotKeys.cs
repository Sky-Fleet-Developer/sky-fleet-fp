using System;
using Core.Utilities;
using Core.GameSetting;
using UnityEngine;
using Core.Boot_strapper;
using System.Threading.Tasks;

namespace Core
{
    public class HotKeys : Singleton<HotKeys>, ILoadAtStart
    {
        public event Action FastSave;

        public event Action SetPause;

        public InputButtons fastSaveBut;

        public InputButtons setPaseBut;

        public static bool IsBlocks { get; set; }

        public Task Load()
        {
            fastSaveBut = InputControl.Instance.GetInput<InputButtons>("General", "Fast save");
            setPaseBut = InputControl.Instance.GetInput<InputButtons>("General", "Set pause");
            return Task.CompletedTask;
        }

        void Update()
        {
            if (!IsBlocks)
            {
                if (InputControl.Instance.GetButtonDown(fastSaveBut) > 0)
                {
                    FastSave?.Invoke();
                }
                if (InputControl.Instance.GetButtonDown(setPaseBut) > 0)
                {
                    SetPause?.Invoke();
                }
            }
        }
    }
}