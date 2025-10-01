using Core.Character;
using Core.Data.GameSettings;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using static Core.Structure.CycleService;
using InputControl = Core.Data.GameSettings.InputControl;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class ControlAxis : IControlElement
    {
        public string computerInput;
        [SerializeField] private bool enableInteraction;
        [SerializeField] protected InputAction bindings;
        [SerializeField, DrawWithUnity] private PortType portType = PortType.Thrust;
        private Port<float> port;
        public bool EnableInteraction => enableInteraction;
        public Transform Root => _device?.transform;
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Port<float>>)value; }
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
        }
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }

        public void Init(IGraphHandler graph, IDriveInterface block)
        {
            GetPort();
        }

        public void Enable()
        {
            bindings.Enable();
        }
        public void Disable()
        {
            bindings.Disable();
        }
        [Space]
        [ShowInInspector, Range(-1, 1)] private float _inputValue;


        [SerializeField, HideInInspector]
        private DeviceBase<Port<float>> _device;

        public void Tick()
        {
            if (bindings.IsInProgress())
            {
                _inputValue = bindings.ReadValue<float>();
            }

            port.Value = _inputValue;
            if (_device)
            {
                _device.Port.Value = _inputValue;
            }
        }
    }
}
