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
        public string GetName()
        {
            return computerInput;
            /*
            if (keyDetected.IsNone())
            {
                return computerInput;
            }
            else
            {
                string res = computerInput;
                for(int i = 0; i < keyDetected.Keys.Count;i++)
                {
                    res += " ";
                    res += keyDetected.Keys[i].ToString();                 
                }
                return res;
            }*/
        }
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

        public void Init(IGraph graph, IDriveInterface block)
        {
            //graph.ConnectPorts(new PortPointer(block, _device.Port, GetName(), nameof(port)), );
            bindings.performed += OnPerformed;   
        }

        private void OnPerformed(InputAction.CallbackContext obj)
        {
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

        public void Tick()
        {
            if (repeatWhenHeld && bindings.IsPressed())
            {
                port.Call();
                _device?.Port.Call();
            }
        }
    }
}