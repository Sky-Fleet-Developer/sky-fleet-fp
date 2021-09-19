using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class HelmDevice : DeviceBase<float>, IArrowDevice
    {
        public Transform handle;
        public float mul = 30;
        public float trim;

        public Vector3 eulerStart;
        public Vector3 axe = Vector3.right;
        
        public override void UpdateDevice()
        {
            handle.localRotation = Quaternion.Euler(eulerStart) * Quaternion.AngleAxis(wire.value * mul + trim, axe);
        }
    }
}
