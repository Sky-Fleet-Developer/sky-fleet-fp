using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class KeyInput
    {
        public enum ButtonControlType
        {
            None = 0,
            BaseKey = 1,
            MouseButton = 2,
            OtherButton = 3,
        }

        public ButtonControlType ControlType { get => _controlType; }

        [SerializeField, HideInInspector]
        private ButtonControlType _controlType;

        [SerializeField, ShowInInspector]
        private KeyCode keyCode;

        [SerializeField, HideInInspector]
        private int mouseButton = 0;


        [SerializeField, HideInInspector]
        private string otherButton;

        public KeyCode GetKeyCode() => keyCode;

        public int GetMouseButton() => mouseButton;

        public string GetOtherButton() => otherButton;

        [Button]
        public void Clear()
        {
            keyCode = KeyCode.None;
            _controlType = ButtonControlType.None;
        }

        [Button]
        public void SetBaseKey(KeyCode code)
        {
            keyCode = code;
            _controlType = ButtonControlType.BaseKey;
        }

        [Button]
        public void SetMouseButton(int code)
        {
            mouseButton = code;
            _controlType = ButtonControlType.MouseButton;
        }

        [Button]
        public void SetOtherButton(string button)
        {
            otherButton = button;
            _controlType = ButtonControlType.OtherButton;
        }

        public bool IsNone()
        {
            return ControlType.Equals(ButtonControlType.None);
        }

        public bool GetButtonDown()
        {
            switch (ControlType)
            {
                case ButtonControlType.BaseKey:
                    return Input.GetKeyDown(keyCode);
                case ButtonControlType.MouseButton:
                    return Input.GetMouseButtonDown(mouseButton);
                case ButtonControlType.OtherButton:
                    return Input.GetButtonDown(otherButton);
                default:
                    return false;
            }
        }
        public bool GetButtonUp()
        {
            switch (ControlType)
            {
                case ButtonControlType.BaseKey:
                    return Input.GetKeyUp(keyCode);
                case ButtonControlType.MouseButton:
                    return Input.GetMouseButtonUp(mouseButton);
                case ButtonControlType.OtherButton:
                    return Input.GetButtonUp(otherButton);
                default:
                    return false;
            }
        }
        public bool GetButton()
        {
            switch (ControlType)
            {
                case ButtonControlType.BaseKey:
                    return Input.GetKey(keyCode);
                case ButtonControlType.MouseButton:
                    return Input.GetMouseButton(mouseButton);
                case ButtonControlType.OtherButton:
                    return Input.GetButton(otherButton);
                default:
                    return false;
            }
        }
    }
}