using System;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Structure.Rigging.Power;
using Runtime.Structure.Ship;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.Structure.Rigging.Movement
{
    public class ClimbingEngine : PowerUserBlock, IForceUser, IFuelUser, IUpdatableBlock
    {
        [SerializeField] private float rotorMass = 1;
        [SerializeField] private AnimationCurve targetRpmPerFuel;
        [SerializeField] private AnimationCurve fuelPerThrottle;
        [SerializeField] private AnimationCurve torquePerDeltaRpm;
        [SerializeField] private float screwPitchDeg = 10;
        [SerializeField] private float fuelConsumptionMul = 1;
        [SerializeField] private AnimationCurve viscousForcePerPower;
        [SerializeField] private float viscousForceMul;
        [SerializeField] private AnimationCurve relaxationRatePerPower;
        [SerializeField] private float relaxationRateMul;
        [SerializeField] private AnimationCurve stiffnessPerPower; 
        [SerializeField] private float stiffnessMul = 50.0f; 
        [SerializeField] private float powerConsumption = 1;

        [ShowInInspector, ReadOnly] private float _radiansPerSecond;
        [ShowInInspector, ReadOnly] private float _consumedFuel;
        private float _massInv;
        private DynamicStructure _structure;
        private MediumState _mediumState;
        private float _powerValue;

        public Port<float> throttle = new Port<float>(PortType.Thrust);
        public Port<float> screwPitchHandle = new Port<float>(PortType.Thrust);
        public Port<float> powerHandle = new Port<float>(PortType.Signal);
        public StoragePort fuel = new StoragePort(typeof(Hydrogen));
        public Port<float> rpm = new Port<float>(PortType.Signal);

        public override float Consumption => powerConsumption * powerHandle.Value;
        
        public struct MediumState
        {
            public float accumulatedNormalStrain;
        }
        
        public struct PropellerOutput
        {
            public Vector3 force;
            public Vector3 torque;
            public float angularAcceleration;
        }

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            _massInv = 1f / rotorMass;
            _structure = (DynamicStructure)structure;
        }

        public void UpdateBlock()
        {
            rpm.Value = _radiansPerSecond / Mathf.PI * 0.5f;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _massInv = 1f / rotorMass;
        }

        public void FuelTick()
        {
            float amount = Mathf.Clamp(fuelPerThrottle.Evaluate(throttle.Value) * fuelConsumptionMul, 0f, fuel.Value);
            _consumedFuel = amount;
            fuel.Value -= amount.DeltaTime();
            float targetRpm = targetRpmPerFuel.Evaluate(_consumedFuel);
            float deltaRpm = targetRpm - _radiansPerSecond;
            float torque = torquePerDeltaRpm.Evaluate(deltaRpm);
            _radiansPerSecond += torque.DeltaTime() * _massInv;
        }

        public void ApplyForce()
        {
            float relaxationRateByPower = relaxationRatePerPower.Evaluate(powerHandle.Value) * relaxationRateMul;
            CalculateViscoElasticForces(ref _mediumState, transform, _structure.Velocity, _radiansPerSecond, screwPitchDeg * screwPitchHandle.Value * Mathf.Deg2Rad, 1, _massInv, relaxationRateByPower, 1f.DeltaTime(), out var output);
            _structure.AddForce(output.force, transform.position);
            _structure.AddTorque(output.torque);
            _radiansPerSecond += output.angularAcceleration * Time.deltaTime;
        }

        /// <summary>
        /// Calculates forces in a viscoelastic medium where work accumulates as spatial strain.
        /// </summary>
        /// <param name="state">Ref to persistent strain state (modified during evaluation).</param>
        /// <param name="relaxationRate">How fast the medium dissipates strain [0..1 per second]. 0 = solid rubber, 20 = fluid.</param>
        /// <param name="deltaTime">Time step (Time.fixedDeltaTime).</param>
        private void CalculateViscoElasticForces(
            ref MediumState state,
            in Transform propellerTransform,
            in Vector3 velocity,
            float angularVelocity,     // rad/s
            float pitchRadians,
            float radius,
            float rotorInertiaInv,
            float relaxationRate,
            float deltaTime,
            out PropellerOutput output)
        {
            float sinPitch = Mathf.Sin(pitchRadians);
            float cosPitch = Mathf.Cos(pitchRadians);

            // 1. World velocity to propeller Z-forward local space
            Vector3 localVel = propellerTransform.InverseTransformDirection(velocity);
            float tangentialSpeed = angularVelocity * radius;

            // 2. Instantaneous penetration speed along the blade normal
            float speedAlongNormal = (localVel.z * cosPitch) + (tangentialSpeed * sinPitch);

            // 3. Accumulate spatial strain (meters of medium compression)
            // If the propeller spins without moving forward, speedAlongNormal > 0 -> strain builds up.
            state.accumulatedNormalStrain += speedAlongNormal * deltaTime;

            // 4. Medium relaxation (strain decays over time due to viscosity/flow)
            // Using exponential decay for frame-rate independent dissipation
            state.accumulatedNormalStrain *= Mathf.Exp(-relaxationRate * deltaTime);

            // 5. Get reaction force from BOTH instantaneous speed AND accumulated compression
            float normalForceSingle = GetReactionForce(speedAlongNormal, state.accumulatedNormalStrain);

            // 6. Decompose to axial thrust (Z) and rotational drag (tangential)
            float axialThrust = -normalForceSingle * cosPitch;
            float tangentialForce = -normalForceSingle * sinPitch;
            float axialTorque = tangentialForce * radius;

            // 7. World-space output vectors
            Vector3 forwardAxis = propellerTransform.forward;
            output.force = forwardAxis * axialThrust;
            output.torque = forwardAxis * axialTorque;

            // 8. Rotational acceleration (alpha = Tau / I)
            output.angularAcceleration = (rotorInertiaInv > 0.0001f) ? (axialTorque * rotorInertiaInv) : 0f;
        }

        /// <summary>
        /// Combined Visco-Elastic reaction force curve.
        /// </summary>
        /// <param name="speedAlongNormal">Instantaneous normal velocity (m/s)</param>
        /// <param name="accumulatedStrain">Accumulated medium compression (meters)</param>
        private float GetReactionForce(float speedAlongNormal, float accumulatedStrain)
        {
            // Viscous damping (resistance to speed)
            float viscousForce = speedAlongNormal * Mathf.Abs(speedAlongNormal) * viscousForcePerPower.Evaluate(powerHandle.Value) * viscousForceMul;

            // Elastic tension/compression (Hooke's law equivalent for the medium)
            // Stiffness coefficient (e.g., 50.0f) determines how hard the medium pushes back when compressed.
            float elasticForce = accumulatedStrain * stiffnessPerPower.Evaluate(powerHandle.Value) * stiffnessMul;

            return viscousForce + elasticForce;
        }
    }
}