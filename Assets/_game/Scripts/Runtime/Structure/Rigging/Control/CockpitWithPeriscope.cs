using System;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class CockpitWithPeriscope : SimpleCockpit, IAimingInterface
    {
        public Port<Vector3> inputTargetPoint = new Port<Vector3>(PortType.Signal);
        private Port<Vector3> outputTargetPoint = new Port<Vector3>(PortType.Signal);
        private ActionPort resetCorrection = new ActionPort();
        private Vector2 inputAngles;
        private AimingInterfaceState currentState = AimingInterfaceState.Default;
        public AimingInterfaceState CurrentState => currentState;
        public Vector3 Target => inputTargetPoint.GetValue();
        public Vector2 Input
        {
            get => inputAngles;
            set => inputAngles = value;
        }
        public event Action OnStateChanged;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            resetCorrection.AddRegisterAction(ResetCorrection);
        }

        private void ResetCorrection()
        {
            inputAngles = Vector2.zero;
        }

        public bool SetState(AimingInterfaceState state)
        {
            currentState = state;
            OnStateChanged?.Invoke();
            return true;
        }


        public override void UpdateBlock(int lod)
        {
            base.UpdateBlock(lod);
            var rotation = Quaternion.Euler(inputAngles.y, inputAngles.x, 0);
            outputTargetPoint.SetValue(rotation * inputTargetPoint.GetValue());
        }
    }
}