using System;
using System.Collections.Generic;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;


namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlToggle : IVisibleControlElement
    {
        [HideInInspector]
        public Port PortAbstact { get => Port; }

        [ShowInInspector]
        public Port<bool> Port;

        [ShowInInspector]
        public DeviceBase Device { get => _device; set => _device = value; }

        [SerializeField, HideInInspector]
        private DeviceBase _device;

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
                Port.Value = isOn;
            }
        }
    }
}