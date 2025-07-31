using Core.Character;
using Core.Data.GameSettings;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class ControlAxis : IControlElement
    {
        public bool EnableInteraction => enableInteraction;
        [SerializeField] private bool enableInteraction;

        public Port GetPort()
        {
            if (port == null || port.ValueType != portType)
            {
                port = new Port<float>(portType);
            }

            return port;
        }
        public string GetName()
        {
            return computerInput;
            /*
            string keysDescr = string.Empty;
            if (!keyPositive.IsNone())
            {
                for (int i = 0; i < keyPositive.Keys.Count; i++)
                {
                    keysDescr += keyPositive.Keys[i].ToString() + " ";
                }
            }
            if (!keyNegative.IsNone())
            {
                keysDescr += ",";
                for (int i = 0; i < keyNegative.Keys.Count; i++)
                {
                    keysDescr += keyNegative.Keys[i].ToString() + " ";
                }
            }
            if (!axe.IsNone()) keysDescr += "," + axe.Axis.ToString();

            return keysDescr.Length == 0 ? computerInput : $"{computerInput} ({keysDescr})";*/
        }
        public Transform Root => _device?.transform;
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }

        private Port<float> port;

        [SerializeField, DrawWithUnity] private PortType portType = PortType.Thrust;

        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Port<float>>)value; }

        public void Init(IGraphHandler graph, ICharacterInterface block)
        {
            GetPort();
            AxisTick();
        }

        [SerializeField] protected InputButtons keyPositive;
        [SerializeField] protected InputButtons keyNegative;
        [SerializeField] protected InputControl.CorrectInputAxis axe;
        [SerializeField] protected InputButtons keyActiveAxis;
        [Space]
        public string computerInput;
        [Space]
        [SerializeField, Range(-1, 1)] protected float inputValue;
        [SerializeField, Range(-1, 1)] protected float realValue;
        [SerializeField, Range(-1, 1)] protected float logicValue;

        [SerializeField, Range(0.1f, 4f)] protected float multiply = 1;
        [SerializeField, Range(0, 20)] protected float sensitivity = 1;
        [SerializeField, Range(0, 5)] protected float gravity = 1;
        [SerializeField, Range(0.5f, 4f)] protected float power = 1;
        [SerializeField, Range(0f, 1f)] protected float dead;
        [SerializeField] protected float trim;
        [SerializeField, Range(0f, 1f)] protected float step;
        [SerializeField] protected bool inverse;
        [SerializeField] protected bool saveInactive;
        [SerializeField, DrawWithUnity] protected AxeType axeType;
        [SerializeField] protected bool fromZeroToOne;


        [SerializeField, HideInInspector]
        private DeviceBase<Port<float>> _device;

        private float GetLogicValue()
        {
            float sign = Mathf.Sign(realValue);
            float val = Clamp(Mathf.Pow(realValue * sign, power) * sign * multiply + trim);
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
            inputValue = InputControl.Instance.GetButton(keyPositive) - InputControl.Instance.GetButton(keyNegative);
            if (axe.IsNone()) return;
            float joy = axe.GetInputSum();
            inputValue = Mathf.Abs(joy) > Mathf.Abs(inputValue) ? joy : inputValue;
        }

        private bool axeDown;
        private int ReadAxeStep()
        {
            if (axe.IsNone()) return 0;
            float axeVal = axe.GetInputSum();
            
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

        public void MoveValueInteractive(float val)
        {
            realValue += val * sensitivity;
            AxisTick();
        }

        public void Tick()
        {
            if (!keyActiveAxis.IsNone() && InputControl.Instance.GetButton(keyActiveAxis) == 0)
            {
                if (saveInactive) return;
                inputValue = 0;
                realValue = 0;
                logicValue = 0;
                port.Value = logicValue;
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
                    realValue = Clamp(realValue + inputValue * sensitivity * DeltaTime);
                    break;
                case AxeType.Steps:
                    int val = ReadAxeStep();
                    int keysValue = val + (int)InputControl.Instance.GetButtonDown(keyPositive) - (int)InputControl.Instance.GetButton(keyNegative);
                    inputValue = Clamp(inputValue + step * keysValue);
                    Dumping();
                    break;
            }

            AxisTick();
        }

        private void AxisTick()
        {
            logicValue = GetLogicValue();
            port.Value = logicValue;
            if (_device)
            {
                _device.Port.Value = logicValue;
            }
        }

        public IInteractiveBlock Block { get; }
    }
}
