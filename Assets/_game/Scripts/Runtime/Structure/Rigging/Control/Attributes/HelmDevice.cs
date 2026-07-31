using System;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class HelmDevice : SingleDevice, IArrowDevice
    {
        public Transform Arrow => lever;
        [SerializeField] private Transform lever;
        [SerializeField] private PortType portType;
        
        public float mul = 30;
        public float trim;

        public Vector3 eulerStart;
        [FormerlySerializedAs("axe")] public Vector3 axis = Vector3.right;

        public override void MoveValueInteractive(float val)
        {
            base.MoveValueInteractive(val);
            UpdateDevice();
        }

        public override void UpdateDevice()
        {
            lever.localRotation = Quaternion.Euler(eulerStart) * Quaternion.AngleAxis(port.Value * mul + trim, axis);
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
