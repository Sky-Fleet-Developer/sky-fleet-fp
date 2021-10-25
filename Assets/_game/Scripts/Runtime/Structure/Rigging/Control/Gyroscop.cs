using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Structure.Rigging.Control.Attributes;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class Gyroscop : Block
    {
        public Port<float>[] outputs = new Port<float>[8];

        private IDynamicStructure root;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            if (structure is IDynamicStructure dynamicStructure) root = dynamicStructure;

            for(int  i = 0; i < 8; i++)
            {
                outputs[i] = new Port<float>(PortType.Thrust);
            }
        }


        private void Update()
        {
            Vector3 localSpeed = root.transform.InverseTransformVector(root.Physics.velocity);
            Vector3 localAngleSpeed = root.transform.InverseTransformVector(root.Physics.angularVelocity);
            float pitch = -Mathf.Asin(root.transform.forward.y) * Mathf.Rad2Deg;
            float roll = -Mathf.Atan2(root.transform.right.y, root.transform.up.y) * Mathf.Rad2Deg;
            outputs[0].Value = localSpeed.x;
            outputs[1].Value = localSpeed.y;
            outputs[2].Value = localSpeed.z;
            outputs[3].Value = localAngleSpeed.x;
            outputs[4].Value = localAngleSpeed.y;
            outputs[5].Value = localAngleSpeed.z;
            outputs[6].Value = pitch;
            outputs[7].Value = roll;
            Debug.Log( localSpeed.ToString() + " " + localAngleSpeed.ToString() + " :" + pitch + " " + roll);
        }
    }
}