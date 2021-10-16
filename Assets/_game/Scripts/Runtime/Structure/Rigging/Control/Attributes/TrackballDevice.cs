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
        [SerializeField] private Transform visualTransfrom;
        public float mul = 30;
        public Vector2 trim;

        public override void UpdateDevice()
        {
            visualTransfrom.localRotation =  Quaternion.Euler(wire.value.x * mul + trim.x, 0, wire.value.y * mul + trim.y);
        }
    }
}