using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Unity.Cinemachine;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class Periscope : BlockWithNode, IAimingDevice, IUpdatableBlock
    {
        [SerializeField] private Transform horizontalAxis;
        [SerializeField] private Transform verticalAxis;
        [SerializeField] private float rotationSpeed;
        private AimingInterfacePort aimingInterfacePort;
        private Port<Vector3> targetPoint = new (PortType.Signal);
        [SerializeField] private CinemachineCamera camera;
        private AimingInterfaceState currentState = AimingInterfaceState.Default;

        public Periscope()
        {
            aimingInterfacePort = new AimingInterfacePort(this);
        }
        
        public void UpdateBlock(int lod)
        {
            if (aimingInterfacePort.Initialized)
            {
                //aimingInterfacePort.Binding.AimingInterface.Input
                Vector3 target = targetPoint.GetValue();
                float horizontalAngle = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg + aimingInterfacePort.Binding.AimingInterface.Input.x;
                float verticalAngle = Mathf.Atan2(target.y, Mathf.Sqrt(target.x * target.x + target.z * target.z)) * Mathf.Rad2Deg + aimingInterfacePort.Binding.AimingInterface.Input.y;
                
                Quaternion horizontalRotation = Quaternion.Euler(Vector3.up * horizontalAngle);
                Quaternion verticalRotation = Quaternion.Euler(Vector3.right * verticalAngle);
                horizontalAxis.localRotation = Quaternion.RotateTowards(horizontalAxis.localRotation, horizontalRotation, rotationSpeed * StructureUpdateModule.DeltaTime);
                verticalAxis.localRotation = Quaternion.RotateTowards(verticalAxis.localRotation, verticalRotation, rotationSpeed * StructureUpdateModule.DeltaTime);;
            }
        }

        public void SetState(AimingInterfaceState state)
        {
            currentState = state;
            camera.enabled = state == AimingInterfaceState.Aiming;
        }
    }
}