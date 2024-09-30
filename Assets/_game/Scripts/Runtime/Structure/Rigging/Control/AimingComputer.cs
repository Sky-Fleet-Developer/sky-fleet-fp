using Core.Graph.Wires;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class AimingComputer : Computer
    {
        private Port<float> horizontalAngleControl = new Port<float>(PortType.Signal);
        private Port<float> horizontalAngleFeedback = new Port<float>(PortType.Signal);
        private Port<float> horizontalVelocityFeedback = new Port<float>(PortType.Signal);
        
        private Port<float> verticalAngleControl = new Port<float>(PortType.Signal);
        private Port<float> verticalAngleFeedback = new Port<float>(PortType.Signal);
        private Port<float> verticalVelocityFeedback = new Port<float>(PortType.Signal);
        
        public Port<Vector3> targetPoint = new Port<Vector3>(PortType.Signal);
        private Port<Vector3> targetVelocity = new Port<Vector3>(PortType.Signal);

        
        protected override void UpdateComputer()
        {
            var target = targetPoint.GetValue();
            if (target.sqrMagnitude == 0)
            {
                horizontalAngleControl.SetValue(0);
                verticalAngleControl.SetValue(0);
                return;
            }
            float horizontalAngle = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;
            float verticalAngle = Mathf.Atan2(target.y, Mathf.Sqrt(target.x * target.x + target.z * target.z)) * Mathf.Rad2Deg;
            horizontalAngleControl.SetValue(horizontalAngle);
            verticalAngleControl.SetValue(verticalAngle);
        }
    }
}