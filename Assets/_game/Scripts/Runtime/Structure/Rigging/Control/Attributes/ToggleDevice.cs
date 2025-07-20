using System;
using System.Collections;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ToggleDevice : DeviceBase<Port<bool>>
    {
        public override Port<bool> Port => port;
        private Port<bool> port = new (PortType.Thrust);
        [SerializeField] private Transform ball;
        [SerializeField, Range(0, 2.0f)] private float minPos;

        public override void UpdateDevice()
        {
            if (port.Value)
            {
                ball.localPosition = new Vector3(0, -minPos, 0);
            }
            else
            {
                ball.localPosition = Vector3.zero;
            }
        }
    }
}
