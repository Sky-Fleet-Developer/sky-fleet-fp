using System;
using Core.Data.GameSettings;
using Core.Graph;
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

        public Port GetPort() => port;

        private ActionPort port = new ActionPort();

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<ActionPort>)value; }

        public void Init(IGraph graph, IControl block)
        {
            //graph.ConnectPorts(new PortPointer(block, _device.Port, GetName(), nameof(port)), );
        }

        [SerializeField, HideInInspector]
        private DeviceBase<ActionPort> _device;

        [SerializeField] protected InputButtons keyDetected;

        public void Tick()
        {
            if(InputControl.Instance.GetButtonDown(keyDetected) > 0)
            {
                port.Call();
                _device.Port.Call();
            }
        }
    }
}