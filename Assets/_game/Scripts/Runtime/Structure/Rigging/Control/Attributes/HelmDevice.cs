using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class HelmDevice : DeviceBase<Port<float>>, IArrowDevice
    {
        public Transform Arrow => lever;
        [SerializeField] private Transform lever;
        
        public float mul = 30;
        public float trim;

        public Vector3 eulerStart;
        public Vector3 axe = Vector3.right;

        public override void UpdateDevice()
        {
            lever.localRotation = Quaternion.Euler(eulerStart) * Quaternion.AngleAxis(port.Value * mul + trim, axe);
        }

        public override Port<float> Port => port;
        private Port<float> port = new Port<float>(PortType.Thrust);
    }
}
