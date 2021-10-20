using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.GameSetting
{
    [System.Serializable]
    public class ControlSetting
    {
        public class CategoryInputs : INameSetting
        {
            public string Name { get; set; }

            public List<INameSetting> Inputs { get; set; }
        }
    }

    public interface INameSetting
    {
        string Name { get; set; }
    }


    [System.Serializable]
    public class InputButtons : INameSetting
    {
        public string Name { get; set; }

        public List<ButtonInput> Keys => keys;

        private List<ButtonInput> keys;

        public void AddKey(ButtonInput key)
        {
            keys.Add(key);
        }

        public void SetKey(ButtonInput key)
        {
            keys.Clear();
            keys.Add(key);
        }
    }

    [System.Serializable]
    public class AxisInput : INameSetting
    {

        public string Name { get; set; }

        public string GetNameAxis() => nameAxis;

        private string nameAxis;

        public void SetAxis(string name)
        {
            nameAxis = name;
        }

        public bool IsNone()
        {
            return string.IsNullOrEmpty(nameAxis);
        }
    }

    [System.Serializable]
    public class ButtonInput
    { 
        public KeyCode GetKeyCode() => keyCode;

        private KeyCode keyCode = KeyCode.None;

        public void Clear()
        {
            keyCode = KeyCode.None;
        }

        public void SetKeyCode(KeyCode key)
        {
            keyCode = key;
        }

        public bool IsNone()
        {
            return keyCode == KeyCode.None;
        }
    }
}