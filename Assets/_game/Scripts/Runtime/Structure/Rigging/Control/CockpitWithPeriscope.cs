using System;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class CockpitWithPeriscope : SimpleCockpit, IAimingInterface
    {
        public AimingInterfacePort aimingInterfacePort;
        public Port<Vector2> correction = new Port<Vector2>(PortType.Signal);
        private ActionPort resetCorrection = new ActionPort();
        private Vector2 inputAngles;
        private AimingInterfaceState currentState = AimingInterfaceState.Default;
        public AimingInterfaceState CurrentState => currentState;
        public Vector2 Input
        {
            get => inputAngles;
            set => inputAngles = value;
        }
        public event Action OnStateChanged;

        public CockpitWithPeriscope()
        {
            aimingInterfacePort = new AimingInterfacePort(this);
        }

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
            aimingInterfacePort.Binding.AimingDevice.SetState(state);
            /*switch (state)
            {
                case AimingInterfaceState.Aiming:
                    
                    break;
            }*/
            currentState = state;
            OnStateChanged?.Invoke();
            return true;
        }


        public override void UpdateBlock()
        {
            base.UpdateBlock();
            //var rotation = Quaternion.Euler(inputAngles.y, inputAngles.x, 0);
            correction.SetValue(inputAngles);
        }
    }
}