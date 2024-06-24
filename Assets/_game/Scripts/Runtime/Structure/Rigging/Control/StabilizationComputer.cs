using System;
using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class StabilizationComputer : Computer
    {
        [SerializeField] private float pitchApex = 10;
        [SerializeField] private float rollApex = 10;
        [SerializeField] private float pitchDumping = 10;
        [SerializeField] private float rollDumping = 10;
        [SerializeField] private float yawDumping = 10;
        [SerializeField] private float maxStrafeCorrectionVelocity = 10;
        [SerializeField, PortGroup("Input")] private Port<bool> relativeRoll = new Port<bool>(PortType.Toggle);
        [SerializeField, PortGroup("Input")] private Port<bool> relativePitch = new Port<bool>(PortType.Toggle);
        [SerializeField, PortGroup("Input")] private Port<float> inputPitch = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> inputRoll = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> inputYaw = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroSpeedX = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroPitch = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroRoll = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroAngularSpeedY = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroAngularSpeedZ = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Input")] private Port<float> gyroAngularSpeedX = new Port<float>(PortType.Thrust);
        
        [SerializeField, PortGroup("Output")] private Port<float> pitch = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Output")] private Port<float> roll = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Output")] private Port<float> yaw = new Port<float>(PortType.Thrust);
        [SerializeField, PortGroup("Output")] private Port<float> strafe = new Port<float>(PortType.Thrust);

        
        protected override void UpdateComputer()
        {
            float overwritePitch = Mathf.Clamp((relativePitch.GetValue() ? gyroPitch.GetValue() / pitchApex : 0) - gyroAngularSpeedX.GetValue() / pitchDumping, -1, 1);
            float strafeValue = Mathf.Clamp(gyroSpeedX.GetValue() / maxStrafeCorrectionVelocity, -1, 1);
            float overwriteRoll = Mathf.Clamp((relativeRoll.GetValue() ? -gyroRoll.GetValue() / rollApex : 0) + gyroAngularSpeedZ.GetValue() / rollDumping, -1, 1);
            float overwriteYaw = Mathf.Clamp(gyroAngularSpeedY.GetValue() / yawDumping, -1, 1);
            float p = Mathf.Clamp(inputPitch.GetValue(), -1, 1);
            float r = Mathf.Clamp(inputRoll.GetValue(), -1, 1);
            float y = Mathf.Clamp(inputYaw.GetValue(), -1, 1);

            pitch.SetValue(p + overwritePitch * (1f - Mathf.Abs(p)));
            roll.SetValue(r + overwriteRoll * (1f - Mathf.Abs(r)));
            yaw.SetValue(y + overwriteYaw * (1f - Mathf.Abs(y)));
            strafe.SetValue(strafeValue);
        }
    }
}