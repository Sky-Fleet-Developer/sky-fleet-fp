using System;
using Core.Utilities;
using Core.GameSetting;
using UnityEngine;
using Core.Boot_strapper;
using System.Threading.Tasks;

namespace Core
{
    public class HotKeys
    {
        public event Action FastSave;

        public event Action SetPause;

        private InputButtons fastSaveBut;

        private InputButtons setPaseBut;



        public HotKeys()
        {
            fastSaveBut = InputControl.Instance.GetInput<InputButtons>("General", "Fast save");
            setPaseBut = InputControl.Instance.GetInput<InputButtons>("General", "Set pause");
        }

        public void Update()
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