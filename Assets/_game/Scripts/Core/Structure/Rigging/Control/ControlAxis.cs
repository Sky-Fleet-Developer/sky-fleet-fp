using Core.Character;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private bool _wasChangedByInput;
        private bool _isInputInProgress;
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

        public void Init(IGraph graph, IDriveInterface block)
        {
            GetPort();
            if (_device)
            {
                Wire wire = port.GetWire();
                if (wire == null)
                {
                    wire = port.CreateWire();
                    port.SetWire(wire);
                }
                _device.Port.SetWire(wire);
            }
            bindings.started += Started;
            bindings.performed += Performed;
            bindings.canceled += Cancelled;
        }

        private void Started(InputAction.CallbackContext obj)
        {
            _isInputInProgress = true;
            _wasChangedByInput = true;
        }
        
        private void Performed(InputAction.CallbackContext obj)
        {
            _wasChangedByInput = true;
        }

        private void Cancelled(InputAction.CallbackContext obj)
        {
            _isInputInProgress = false;
            //_wasChangedByInput = true;
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

        public float Value => _inputValue;
        public void SetValue(float value)
        {
            _inputValue = value;
            port.Value = _inputValue;
        }

        public void Tick()
        {
            if (_wasChangedByInput || _isInputInProgress)
            {
                _inputValue = bindings.ReadValue<float>();
                port.Value = _inputValue;
                _wasChangedByInput = false;
            }
        }
    }
}
