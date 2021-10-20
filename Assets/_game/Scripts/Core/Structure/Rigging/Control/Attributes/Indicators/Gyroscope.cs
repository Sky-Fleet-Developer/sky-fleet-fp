using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class Gyroscope : IndicatorIndependent
    {
        [SerializeField] private Transform sphereBase;
        [SerializeField] private Transform horizontBase;

        private void Update()
        {
            UpdateDevice();
        }
        public override void UpdateDevice()
        {
            float pitch = transform.eulerAngles.x;
            float roll = -transform.eulerAngles.z;
            Quaternion localRot = Quaternion.AngleAxis(pitch, Vector3.right) * Quaternion.AngleAxis(roll, Vector3.forward);
            sphereBase.localRotation = localRot;
            horizontBase.localEulerAngles = new Vector3(0, -transform.eulerAngles.y, 0);
        }
    }
}