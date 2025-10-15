using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class HelmDevice : SingleDevice, IArrowDevice
    {
        public Transform Arrow => lever;
        [SerializeField] private Transform lever;
        [SerializeField][DrawWithUnity] private PortType portType;
        
        public float mul = 30;
        public float trim;

        public Vector3 eulerStart;
        public Vector3 axe = Vector3.right;

        public override void UpdateDevice()
        {
            lever.localRotation = Quaternion.Euler(eulerStart) * Quaternion.AngleAxis(port.Value * mul + trim, axe);
        }

        public override Port<float> Port => port;
        [ShowInInspector, ReadOnly] private Port<float> port;

        public override void Init(IGraph graph, IBlock block)
        {
            if (port == null || portType != port.ValueType)
            {
                port = new(portType);
            }

            base.Init(graph, block);
        }
    }
}
