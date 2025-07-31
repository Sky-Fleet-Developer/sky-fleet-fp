using System;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class TrackballDevice : DeviceBase<Port<Vector2>>
    {
        public override void MoveValueInteractive(float val)
        {
            throw new NotImplementedException();
        }

        public override void ExitControl()
        {
            throw new NotImplementedException();
        }

        public override Port<Vector2> Port => port;
        private Port<Vector2> port = new(PortType.Thrust);
        [SerializeField] private Transform ball;
        public float mul = 30;
        public Vector2 trim;

        public override void UpdateDevice()
        {
            ball.localRotation = Quaternion.Euler(port.Value.x * mul + trim.x, 0, port.Value.y * mul + trim.y);
        }
    }
}