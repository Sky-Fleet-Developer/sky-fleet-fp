using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ToggleDevice : EasyDevice<bool>
    {

        [SerializeField, Range(0, 2.0f)] private float minPos;

        public override void Init(IStructure structure, IBlock block, string port)
        {
            base.Init(structure, block, port);
        }

        public override void UpdateDevice(int lod)
        {
            base.UpdateDevice(lod);
            if (IsMinLod)
                if (wire.value)
                {
                    visualTransfrom.localPosition = new Vector3(0, -minPos, 0);
                }
                else
                {
                    visualTransfrom.localPosition = Vector3.zero;
                }
        }
    }
}
