using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class AxisOption
    {
        public enum SideAxe
        {
            Positive = 0,
            Negative = 1,
        }

        [SerializeField, HideInInspector]
        private string nameAxe;

        [SerializeField, HideInInspector]
        private SideAxe side;

        public string GetNameAxe() => nameAxe;

        [Button]
        public void SetAxe(string name, SideAxe side)
        {
            nameAxe = name;
            this.side = side;
        }

        public float GetValue()
        {
            float val = Input.GetAxisRaw(nameAxe);   
            if (val < 0 && side == SideAxe.Positive)
            {
                val = 0;
            }
            else if (val > 0 && side == SideAxe.Negative)
            {
                val = 0;
            }
            return Mathf.Abs(val);
        }
    }

    [System.Serializable]
    public class ButtonOption
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

    [System.Serializable]
    public class ControlInputSetting
    {
        public enum ControlType
        {
            None = 0,
            Button = 1,
            Axis = 2,
        }

        [SerializeField, ShowInInspector]
        private ControlType type;

        [SerializeField, ShowInInspector]
        private AxisOption axis;

        [SerializeField, ShowInInspector]
        private ButtonOption button;

        public ControlType GetControlType() => type;

        [Button]
        public void SetAxis(AxisOption axis)
        {
            this.axis = axis;
            type = ControlType.Axis;
        }

        [Button]
        public void SetButton(ButtonOption button)
        {
            this.button = button;
            type = ControlType.Button;
        }

        public float GetValue()
        {
            switch (type)
            {
                case ControlType.Axis:
                    return axis.GetValue();
                case ControlType.Button:
                    return button.GetButton() ? 1 : 0;
                default:
                    return 0;
            }
        }
    }

    [System.Serializable]
    public class ControlAxe : IVisibleControlElement
    {
        [SerializeField] protected ControlInputSetting keyPositive;
        [SerializeField] protected ControlInputSetting keyNegative;
        [SerializeField] protected ButtonOption keyActiveAxis;
        [Space]
        public string computerInput;
        [Space]
        [SerializeField] protected float value;


        [SerializeField] protected float multiply;
        [SerializeField] protected float power = 1;
        [SerializeField] protected float speedChange = 0.01f;
        [SerializeField] protected float step;
        [SerializeField] protected float trim;
        [SerializeField] protected float deadZone;
        [SerializeField] protected bool inverseAxe;
        [SerializeField] protected bool saveValue;

        private float cashVal;

        public float GetValue() => value;

        [ShowInInspector]
        public Port<float> Port;


        [ShowInInspector]
        public DeviceBase Device { get => _device; set => _device = value; }

        [SerializeField, HideInInspector]
        private DeviceBase _device;

        public void Tick()
        {
            if(keyActiveAxis.ControlType != ButtonOption.ButtonControlType.None && !keyActiveAxis.GetButton())
            {
                if(!saveValue)
                {
                    cashVal = trim;
                    value = trim;
                    Port.Value = value;
                }
                return;
            }
            float vBut = (keyPositive.GetValue() - keyNegative.GetValue());

            bool isAllButton = keyPositive.GetControlType() == ControlInputSetting.ControlType.Button && keyNegative.GetControlType() == ControlInputSetting.ControlType.Button;


            if (isAllButton)
            {
                float v = Mathf.Pow(multiply * speedChange * DeltaTime, power);
                if (inverseAxe)
                    v = -v;
                if (vBut != 0)
                {                
                    v *= vBut;
                    cashVal += v;
                }
                else
                {
                    float r = Mathf.Sign(cashVal) * v;
                    if (Mathf.Abs(r) > Mathf.Abs(cashVal))
                    {
                        cashVal = 0;
                    }
                    else
                    {
                        cashVal -= r;
                    }
                }
            }
            else
            {
                float v = vBut * Mathf.Pow(multiply, power);
                if (inverseAxe)
                    v = -v;
                cashVal = v;
            }
            cashVal = Mathf.Clamp(cashVal, -1, 1);
            if (Mathf.Abs(cashVal) <= deadZone)
            {
                value = trim;
            }
            else
            {
                value = Mathf.Clamp(cashVal + trim, -1, 1);
            }
            if (step > 0)
                value = Mathf.Ceil(value * (1 / step)) * step;
            Port.Value = value;
        }

    }
}
