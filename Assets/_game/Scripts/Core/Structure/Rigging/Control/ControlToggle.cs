using System;
using Core.Structure.Rigging.Control.Attributes;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlToggle : IControlElement
    {
        public Port GetPort() => port;

        public string GetPortDescription()
        {
            return keyDetected.IsNone() ? computerInput : $"{computerInput} ({keyDetected.GetKeyCode()})";
        }
        
        [SerializeField, ShowInInspector]
        private Port<bool> port = new Port<bool>();

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<bool>)value; }

        public void Init(IStructure structure, IControl block)
        {
            structure.ConnectPorts(port, _device.port);
        }

        [SerializeField, HideInInspector]
        private DeviceBase<bool> _device;

        [SerializeField] protected KeyInput keyDetected;

        private bool isOn;

        public void Tick()
        {
            if (keyDetected.GetButtonDown())
            {
                if (isOn)
                {
                    isOn = false;
                }
                else
                {
                    isOn = true;
                }
                port.Value = isOn;
            }
        }
    }
}