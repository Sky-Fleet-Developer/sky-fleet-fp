using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ToggleDevice : DeviceBase<bool>
    {
        [SerializeField] private Transform ball;
        [SerializeField, Range(0, 2.0f)] private float minPos;

        public override void UpdateDevice(int lod)
        {
            base.UpdateDevice(lod);
            if (lod == 0 && wire.value)
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
