using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class Gyroscope : IndicatorBase<Vector2>
    {
        [SerializeField] private Transform sphereBase;

        public override void Init(IStructure structure, IBlock block, string port)
        {
            base.Init(structure, block, port);
        }

        public override void UpdateDevice()
        {
            float pitch = wire.value.x;
            float roll = wire.value.y;
            Quaternion localRot = Quaternion.AngleAxis(pitch, Vector3.right) * Quaternion.AngleAxis(roll, Vector3.forward);
            sphereBase.localRotation = localRot;
        }
    }
}