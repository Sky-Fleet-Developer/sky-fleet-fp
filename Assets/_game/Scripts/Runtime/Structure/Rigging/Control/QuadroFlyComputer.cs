using System;
using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class QuadroFlyComputer : Computer
    {
        [SerializeField] private Port<float> pitch = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> roll = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroSpeedX = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> gyroSpeedZ = new Port<float>(PortType.Thrust);
        [SerializeField] private Port<float> strafe = new Port<float>(PortType.Thrust);
        [SerializeField] private float rotatePercent = 2;
        [SerializeField] private float strafePercent = 1;

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
                    new PortPointer(this, gyroSpeedX, nameof(gyroSpeedX)),
                    new PortPointer(this, gyroSpeedZ, nameof(gyroSpeedZ)),
                    new PortPointer(this, strafe, nameof(strafe)),
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
            float rollValue = roll.GetValue();
            float pitchValue = pitch.GetValue();
            float strafeValue = strafe.GetValue();

            float forwardSign = Mathf.Sign(gyroSpeedZ.GetValue());
            float sidewaysSign = Mathf.Sign(gyroSpeedX.GetValue());
            float forwardFunction = Mathf.Abs(gyroSpeedZ.GetValue());
            float sidewaysFunction = Mathf.Abs(gyroSpeedX.GetValue());

            float longitudinalTwist = Mathf.Clamp(rollValue * forwardFunction * forwardSign / maxLongitudinalTwistVelocity, -1, 1);
            float longitudinalBend = Mathf.Clamp(pitchValue * forwardFunction / maxLongitudinalBendVelocity, -1, 1);
            float transverseTwist = Mathf.Clamp(-pitchValue * sidewaysFunction * sidewaysSign / maxTraverseTwistVelocity, -1, 1);
            float transverseBend = Mathf.Clamp(rollValue * sidewaysFunction * sidewaysSign / maxTraverseBendVelocity, -1, 1);

            float divider = 1 / (rotatePercent + strafePercent);
            float rotateMul = rotatePercent * divider;
            float strafeMul = strafePercent * divider;
            
            foreach (SupportPosition supportPosition in _supportPositions)
            {
                float supportPitch = supportPosition.Position.x * longitudinalTwist +
                                     supportPosition.Position.y * longitudinalBend;
                float supportRoll = (supportPosition.Position.y * transverseTwist -
                                     supportPosition.Position.x * transverseBend) * rotateMul
                                    +
                                    strafeValue * strafeMul;
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