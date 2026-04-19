using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class ElevonsThrustDistributor : BlockWithNode, IUpdatableBlock
    {
        [SerializeField][DrawWithUnity] private PortType portType;

        [SerializeField, BlockRelativeValue]
        private float pitchInfluence = 0.75f;
        [SerializeField, BlockRelativeValue]
        private float rollInfluence = 0.75f;
        private Port<float> inputPitch;
        private Port<float> inputRoll;
        private Port<float> outputLeft;
        private Port<float> outputRight;

        public override void InitNode(IGraph graph)
        {
            inputPitch ??= new (portType);
            inputRoll ??= new (portType);
            outputLeft ??= new (portType);
            outputRight ??= new (portType);
            base.InitNode(graph);
        }

        public void UpdateBlock()
        {
            float left = inputPitch.Value * pitchInfluence + inputRoll.Value * rollInfluence;
            float right = inputPitch.Value * pitchInfluence - inputRoll.Value * rollInfluence;
            outputLeft.SetValue(left);
            outputRight.SetValue(right);
        }
    }
}