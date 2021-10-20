using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class TrackballDevice : DeviceBase<Vector2>
    {
        [SerializeField] private Transform ball;
        public float mul = 30;
        public Vector2 trim;

        public override void UpdateDevice()
        {
            ball.localRotation = Quaternion.Euler(port.Value.x * mul + trim.x, 0, port.Value.y * mul + trim.y);
        }
    }
}