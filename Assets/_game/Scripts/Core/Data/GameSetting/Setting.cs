using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.GameSetting
{
    [System.Serializable]
    public class Setting
    {
        public ControlSetting Control => _control;

        private ControlSetting _control;

        public Setting()
        {
            _control = new ControlSetting();
        }
    }
}