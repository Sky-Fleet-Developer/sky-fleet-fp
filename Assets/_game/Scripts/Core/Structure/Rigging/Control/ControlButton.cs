using System;
using Core.Data.GameSettings;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Core.Structure.Rigging.Control
{

    [Serializable]
    public class ControlButton : IControlElement
    {
        public bool EnableInteraction => enableInteraction;
        [SerializeField] private bool enableInteraction;
        public string GetName()
        {
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
            }
        }
        public Transform Root => _device.transform;

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

        [SerializeField] protected InputButtons keyDetected;

        public void Tick()
        {
            if(InputControl.Instance.GetButton(keyDetected) > 0)
            {
                port.Value(this);
            }
        }
    }
}