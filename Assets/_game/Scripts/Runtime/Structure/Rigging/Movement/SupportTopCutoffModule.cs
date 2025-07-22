using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Structure.Rigging.Power;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class SupportTopCutoffModule : PowerUserBlock
    {
        [SerializeField] private float consumption;
        [SerializeField, DrawWithUnity] private PortType controlType;
        [SerializeField] private float deadRangeMin;
        [SerializeField] private float deadRangeMax;
        private Port<Vector3> supportLocalForce = new (PortType.Signal);
        private Port<float> inputPower;
        private Port<float> outputPower;
        public override float Consumption => consumption;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            inputPower ??= new Port<float>(controlType);
            outputPower ??= new Port<float>(controlType);
            base.InitBlock(structure, parent);
        }

        public override void PowerTick()
        {
            base.PowerTick();
            if (IsWork)
            {
                float v = supportLocalForce.GetValue().y;
                if (v < deadRangeMax && v > deadRangeMin)
                {
                    outputPower.SetValue(0);
                }
                else
                {
                    outputPower.SetValue(inputPower.Value);
                }
            }
        }
    }
}