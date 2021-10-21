using System;
using Core.Structure.Rigging.Control.Attributes;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Core.Structure.Rigging.Control
{

    [Serializable]
    public class ControlButton : IControlElement
    {
        public string GetPortDescription()
        {
            return keyDetected.IsNone() ? computerInput : $"{computerInput} ({keyDetected.GetKeyCode()})";
        }
        
        public Port GetPort() => port;

        [SerializeField, ShowInInspector]
        private Port<Action<object>> port = new Port<Action<object>>(PortType.Button);

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Action<object>>)value; }

        public void Init(IStructure structure, IControl block)
        {
            //structure.ConnectPorts(port, _device.port);
            _device.port.cache += port.Value;
        }


        [SerializeField, HideInInspector]
        private DeviceBase<Action<object>> _device;

        [SerializeField] protected KeyInput keyDetected;

        public void Tick()
        {
            if(keyDetected.GetButtonDown())
            {
                port.Value(this);
            }
        }
    }
}