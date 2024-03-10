using System;
using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class QuadroFlyComputer : Computer
    {
        [SerializeField] private float maxRollAngle;
        [SerializeField] private Port<float> pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> roll = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> yaw = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroSpeedX = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroSpeedZ = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroPitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroRoll = new Port<float>(PortType.Thrust);

        [SerializeField] private Port<float> support_FR_Pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_FR_Roll = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_FL_Pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_FL_Roll = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_BR_Pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_BR_Roll = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_BL_Pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> support_BL_Roll = new Port<float>(PortType.Thrust);
        [SerializeField] private float maxLongitudinalTwistVelocity;
        [SerializeField] private float maxLongitudinalBendVelocity;
        [SerializeField] private float maxTraverseTwistVelocity;
        [SerializeField] private float maxTraverseBendVelocity;
        [SerializeField] private bool relativeRoll;
        [SerializeField] private bool relativePitch;
        //[SerializeField] private float pitchSmooth;
        //[SerializeField] private float rollSmooth;

        private List<SupportPosition> _supportPositions;

        private QuadroFlyComputer()
        {
            try
            {
                _supportPositions = new List<SupportPosition>
                {
                    new SupportPosition(support_FR_Pitch, support_FR_Roll, new Vector2(1, 1)),
                    new SupportPosition(support_FL_Pitch, support_FL_Roll, new Vector2(-1, 1)),
                    new SupportPosition(support_BR_Pitch, support_BR_Roll, new Vector2(1, -1)),
                    new SupportPosition(support_BL_Pitch, support_BL_Roll, new Vector2(-1, -1)),
                };
                inputsPorts = new List<PortPointer>
                {
                    new PortPointer(this, pitch, nameof(pitch)),
                    new PortPointer(this, roll, nameof(roll)),
                    new PortPointer(this, yaw, nameof(yaw)),
                    new PortPointer(this, gyroSpeedX, nameof(gyroSpeedX)),
                    new PortPointer(this, gyroSpeedZ, nameof(gyroSpeedZ)),
                };
                outputPorts = new List<PortPointer>
                {
                    new PortPointer(this, support_FR_Pitch, "support/FR/Pitch"),
                    new PortPointer(this, support_FR_Roll, "support/FR/Roll"),
                    new PortPointer(this, support_FL_Pitch, "support/FL/Pitch"),
                    new PortPointer(this, support_FL_Roll, "support/FL/Roll"),
                    new PortPointer(this, support_BR_Pitch, "support/BR/Pitch"),
                    new PortPointer(this, support_BR_Roll, "support/BR/Roll"),
                    new PortPointer(this, support_BL_Pitch, "support/BL/Pitch"),
                    new PortPointer(this, support_BL_Roll, "support/BL/Roll"),
                };
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        protected override void UpdateComputer()
        {
            float pitchAngle = gyroPitch.GetValue();
            float rollAngle = gyroRoll.GetValue();
            float roll = this.roll.GetValue();
            float pitch = this.pitch.GetValue();

            float forwardSign = Mathf.Sign(gyroSpeedZ.GetValue());
            float sidewaysSign = Mathf.Sign(gyroSpeedX.GetValue());
            float forwardFunction = Mathf.Abs(gyroSpeedZ.GetValue());
            float sidewaysFunction = Mathf.Abs(gyroSpeedX.GetValue());

            float longitudinalTwist = Mathf.Clamp(roll * forwardFunction * forwardSign / maxLongitudinalTwistVelocity, -1, 1);
            float longitudinalBend = Mathf.Clamp(pitch * forwardFunction / maxLongitudinalBendVelocity, -1, 1);
            float transverseTwist = Mathf.Clamp(pitch * sidewaysFunction / maxTraverseTwistVelocity, -1, 1);
            float transverseBend = Mathf.Clamp(roll * sidewaysFunction * sidewaysSign / maxTraverseBendVelocity, -1, 1);
            
            /*float horizontalStabilizationX = Mathf.Clamp(rollAngle / rollMaxAngle - gyroSpeedX.GetValue() / maxPitchForwardVelocity, -1f, 1f) * (relativeRoll ? 1 : 0) * (1f - Mathf.Abs(roll));
            float horizontalStabilizationZ = Mathf.Clamp(-Mathf.Abs(rollAngle) / rollMaxAngle - Mathf.Abs(gyroSpeedX.GetValue()) / maxPitchForwardVelocity, -1, 1); // * (ReletiveRoll ? 1 : 0);
            float horizontalSpeedStabilizationZ = Mathf.Clamp(gyroSpeedX.GetValue() / maxPitchForwardVelocity, -1f, 1f) * (1 - Mathf.Clamp01(Mathf.Abs(horizontalStabilizationZ)));
            float horizontalRotationX = -roll * gyroSpeedZ.GetValue() / maxPitchForwardVelocity;
            float horizontalRotationZ = Mathf.Clamp(-roll * gyroSpeedX.GetValue() / maxPitchForwardVelocity, -1.5f, 1.5f);

            float verticalRotation = -pitch;
            float verticalStabilization = pitchAngle / gyroPitch.GetValue() * (relativePitch ? 1 : 0) * (1f - Mathf.Abs(pitch)) * Mathf.Sign(gyroSpeedZ.GetValue());*/

            foreach (SupportPosition supportPosition in _supportPositions)
            {
                /*float xRotation = Mathf.Clamp(
                    horizontalStabilizationX * supportPosition.Position.x * gyroSpeedZ.GetValue() /
                    maxPitchForwardVelocity / rollSmooth
                    + horizontalRotationX * supportPosition.Position.x / rollSmooth
                    + verticalRotation * supportPosition.Position.y / pitchSmooth
                    + verticalStabilization * supportPosition.Position.y / pitchSmooth,
                    -1f, 1f);

                float zRotation = Mathf.Clamp(
                    -horizontalStabilizationZ * supportPosition.Position.x / rollSmooth
                    + horizontalSpeedStabilizationZ
                    - horizontalRotationZ * supportPosition.Position.x / rollSmooth,
                    -1f, 1f);*/
                
                //position : y is z
                float supportPitch = supportPosition.Position.x * longitudinalTwist +
                                     supportPosition.Position.y * longitudinalBend;
                float supportRoll = supportPosition.Position.y * transverseTwist -
                                     supportPosition.Position.x * transverseBend;
                supportPosition.SetValues(supportPitch, supportRoll);
            }
        }

        private class SupportPosition
        {
            private Port<float> _pitch;
            private Port<float> _roll;
            private Vector2 _position;

            public SupportPosition(Port<float> pitch, Port<float> roll, Vector2 position)
            {
                _pitch = pitch;
                _roll = roll;
                _position = position;
            }

            public Vector2 Position => _position;

            public void SetValues(float pitch, float roll)
            {
                _pitch.SetValue(pitch);
                _roll.SetValue(roll);
            }
        }
    }
}