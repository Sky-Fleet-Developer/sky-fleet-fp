using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Structure.Rigging.Power;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class ThrustSensor : PowerUserBlock, IUpdatableBlock
    {
        [SerializeField] private PortType inputType;
        public Port<float> input;
        public Port<float> output = new Port<float>(PortType.Signal);

        [SerializeField] private float value = 0;
        public override void InitBlock(IStructure structure, Parent parent)
        {
            input = new(inputType);
            base.InitBlock(structure, parent);
        }

        public void UpdateBlock()
        {
            output.SetValue(input.GetValue());
        }

        public override float Consumption => 0.1f;
    }
}