using Core.Graph.Wires;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class MechanicalReducer : BlockWithNode, IUpdatableBlock
    {
        public Port<float> portA = new Port<float>(PortType.Thrust);
        public Port<float> portB = new Port<float>(PortType.Thrust);

        [SerializeField] private float value = 0;

        public void UpdateBlock(int lod)
        {
            float delta = (portA.Value + portB.Value / value) * 0.5f;
            portA.Value -= delta;
            portB.Value -= delta * value;
        }
    }
}