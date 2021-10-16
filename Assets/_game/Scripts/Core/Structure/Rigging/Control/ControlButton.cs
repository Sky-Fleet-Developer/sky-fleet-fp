using System;
using System.Collections.Generic;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;


namespace Core.Structure.Rigging.Control
{

    [Serializable]
    public class ControlButton : IVisibleControlElement
    {
        [HideInInspector]
        public Port PortAbstact { get => Port; }

        [ShowInInspector]
        public Port<Action<object>> Port;

        [ShowInInspector]
        public DeviceBase Device { get => _device; set => _device = value; }

        [SerializeField, HideInInspector]
        private DeviceBase _device;

        [SerializeField] protected KeyInput keyDetected;

        public void Tick()
        {
            if(keyDetected.GetButtonDown())
            {
                Port.Value(this);
            }
        }
    }
}