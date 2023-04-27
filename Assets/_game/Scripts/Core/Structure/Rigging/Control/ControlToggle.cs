using System;
using Core.Data.GameSettings;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlToggle : IControlElement
    {
        public bool EnableInteraction => enableInteraction;
        [SerializeField] private bool enableInteraction;
        public Port GetPort() => port;
        public Transform Root => _device.transform;

        public string GetPortDescription()
        {
            if (keyDetected.IsNone())
            {
                return computerInput;
            }
            else
            {
                string res = computerInput;
                for (int i = 0; i < keyDetected.Keys.Count; i++)
                {
                    res += " ";
                    res += keyDetected.Keys[i].ToString();
                }
                return res;
            }
        }
        
        [SerializeField, ShowInInspector]
        private Port<bool> port = new Port<bool>();

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<bool>)value; }

        public void Init(IStructure structure, IControl block)
        {
            //structure.ConnectPorts(port, _device.port);
        }

        [SerializeField, HideInInspector]
        private DeviceBase<bool> _device;

        [SerializeField] protected InputButtons keyDetected;

        private bool isOn;

        public void Tick()
        {
            if (InputControl.Instance.GetButtonDown(keyDetected) > 0)
            {
                isOn = !isOn;
                port.Value = isOn;
                _device.port.Value = isOn;
            }
        }
    }
}