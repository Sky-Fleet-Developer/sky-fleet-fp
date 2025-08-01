using System;
using Core.Character;
using Core.Data.GameSettings;
using Core.Graph;
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
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }
        public string GetName()
        {
            return computerInput;
/*            if (keyDetected.IsNone())
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
            }*/
        }
        
        [SerializeField, ShowInInspector]
        private Port<bool> port = new Port<bool>();

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Port<bool>>)value; }

        public void Init(IGraphHandler graph, ICharacterInterface block)
        {
        }

        [SerializeField, HideInInspector]
        private DeviceBase<Port<bool>> _device;

        [SerializeField] protected InputButtons keyDetected;

        private bool isOn;

        public void Tick()
        {
            if (InputControl.Instance.GetButtonDown(keyDetected) > 0)
            {
                isOn = !isOn;
                port.Value = isOn;
                _device.Port.Value = isOn;
            }
        }
    }
}