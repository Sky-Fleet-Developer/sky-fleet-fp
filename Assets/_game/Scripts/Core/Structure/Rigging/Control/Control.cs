using System;
using System.Collections.Generic;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;

namespace Core.Structure.Rigging.Control
{
   
    
    

    [System.Serializable]
    public class ControlAxe : IVisibleControlElement
    {
        [SerializeField] protected KeyInput keyPositive;
        [SerializeField] protected KeyInput keyNegative;
        [SerializeField] protected AxeInput axe;
        [SerializeField] protected KeyInput keyActiveAxis;
        [Space]
        public string computerInput;
        [Space]
        [SerializeField, Range(-1, 1)] protected float inputValue;
        [SerializeField, Range(-1, 1)] protected float realValue;
        [SerializeField, Range(-1, 1)] protected float logicValue;

        [SerializeField, Range(0.1f, 4f)] protected float multiply = 1;
        [SerializeField, Range(0, 5)] protected float sensitivity = 1;
        [SerializeField, Range(0, 5)] protected float gravity = 1;
        [SerializeField, Range(0.5f, 4f)] protected float power = 1;
        [SerializeField, Range(0f, 1f)] protected float dead;
        [SerializeField] protected float trim;
        [SerializeField, Range(0f, 1f)] protected float step;
        [SerializeField] protected bool inverse;
        [SerializeField] protected bool saveInactive;
        [SerializeField] protected AxeType axeType;
        [SerializeField] protected bool fromZeroToOne;
        

        [ShowInInspector]
        public Port<float> Port;


        [ShowInInspector]
        public DeviceBase Device { get => _device; set => _device = value; }

        [SerializeField, HideInInspector]
        private DeviceBase _device;

        private float GetLogicValue()
        {
            float sign = Mathf.Sign(realValue);
            float val =  Clamp(Mathf.Pow(realValue * sign, power) * sign * multiply + trim);
            float result = Mathf.Abs(val) >= dead ? val : 0;

            if (axeType == AxeType.Relative && step > 0f)
            {
                result = Mathf.Ceil(result / step - 0.5f) * step;
            }

            if (inverse) result = -result;
            return result;
        }

        private float Clamp(float value)
        {
            return Mathf.Clamp(value, fromZeroToOne ? 0f : -1f, 1);
        }

        private void ReadInputValue()
        {
            inputValue = (keyPositive.GetButton() ? 1 : 0) + (keyNegative.GetButton() ? -1 : 0);
            if (axe.IsNone()) return;
            float joy = axe.GetValue();
            inputValue = Mathf.Abs(joy) > Mathf.Abs(inputValue) ? joy : inputValue;
        }

        private bool axeDown;
        private int ReadAxeStep()
        {
            if (axe.IsNone()) return 0;
            float axeVal = axe.GetValue();
            if (axeDown)
            {
                if (Mathf.Abs(axeVal) < 0.5f)
                {
                    axeDown = false;
                }
            }
            else
            {
                if (Mathf.Abs(axeVal) > 0.75f)
                {
                    axeDown = true;
                    return (int)Mathf.Sign(axeVal);
                }
            }

            return 0;
        }

        private void Dumping()
        {
            switch (axeType)
            {
                case AxeType.Absolute:
                case AxeType.Relative:
                    float delta = inputValue - realValue;
                    float dumping = Mathf.Abs(inputValue) > dead ? sensitivity : gravity;

                    if (dumping == 0)
                    {
                        realValue = inputValue;
                    }
                    else
                    {
                        float dd = dumping * DeltaTime;
                        realValue += Mathf.Clamp(delta, -dd, dd);
                    }
                    break;
                case AxeType.Steps:
                    if (sensitivity == 0)
                    {
                        realValue = inputValue;
                    }
                    else
                    {
                        realValue = Mathf.MoveTowards(realValue, inputValue, sensitivity * DeltaTime);
                    }
                    break;
            }
            
        }
        
        public void Tick()
        {
            if(!keyActiveAxis.IsNone() && !keyActiveAxis.GetButton())
            {
                if (saveInactive) return;
                inputValue = 0;
                realValue = 0;
                logicValue = 0;
                Port.Value = logicValue;
                return;
            }
            
            switch (axeType)
            {
                case AxeType.Absolute:
                    ReadInputValue();
                    Dumping();
                    realValue = Clamp(realValue);
                    break;
                case AxeType.Relative:
                    ReadInputValue();
                    realValue = Clamp(realValue + inputValue * DeltaTime);
                    break;
                case AxeType.Steps:
                    int val = ReadAxeStep();
                    int keysValue = val + (keyPositive.GetButtonDown() ? 1 : 0) + (keyNegative.GetButtonDown() ? -1 : 0);
                    inputValue = Clamp(inputValue + step * keysValue);
                    Dumping();
                    break;
            }

            logicValue = GetLogicValue();
            Port.Value = logicValue;
        }

    }
    
     [System.Serializable]
    public class AxeInput
    {
        [SerializeField]
        private string nameAxe;

        public string GetNameAxe() => nameAxe;

        [Button]
        public void SetAxe(string name)
        {
            nameAxe = name;
        }

        public float GetValue()
        {
            return Input.GetAxisRaw(nameAxe);   
        }
        
        public bool IsNone()
        {
            return string.IsNullOrEmpty(nameAxe);
        }
    }

    public enum AxeType
    {
        Absolute = 0,
        Relative = 1,
        Steps = 2
    }

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
