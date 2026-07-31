using System;
using Core.Character;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using InputControl = Core.Data.GameSettings.InputControl;


namespace Core.Structure.Rigging.Control
{

    [Serializable]
    public class ControlButton : IControlElement
    {
        private enum CallType {OnStart, OnPress, OnRelease}
        [SerializeField] private CallType callType;
        public Transform Root => _device ? _device.transform : null;
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }

        public Port GetPort() => port;

        private ActionPort port = new ActionPort();

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<ActionPort>)value; }
        public string GetName()
        {
            return computerInput;
        }
        
        public void Init(IGraph graph, IDriveInterface block)
        {
            _block = block;
            //graph.ConnectPorts(new PortPointer(block, _device.Port, GetName(), nameof(port)), );
            switch (callType)
            {
                case CallType.OnStart:
                    bindings.started += Call;   
                    break;
                case CallType.OnPress:
                    bindings.performed += Call;   
                    break;
                case CallType.OnRelease:
                    bindings.started += Call;   
                    break;
            }
        }

        private void Call(InputAction.CallbackContext obj)
        {
            if (_block.RejectDirectInput)
            {
                return;
            }
            port.Call();
            _device?.Port.Call();
        }

        public void Enable()
        {
            bindings.Enable();
        }
        
        public void Disable()
        {
            bindings.Disable();
        }

        [SerializeField, HideInInspector]
        private DeviceBase<ActionPort> _device;
        [SerializeField] private bool repeatWhenHeld;

        [SerializeField] protected InputAction bindings;
        private IDriveInterface _block;

        public void Tick()
        {
            if (_block.RejectDirectInput)
            {
                return;
            }
            if (repeatWhenHeld && bindings.IsPressed())
            {
                port.Call();
                _device?.Port.Call();
            }
        }
    }
}