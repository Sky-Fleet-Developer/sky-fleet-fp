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
        [SerializeField] private float maxRpm = 150;
        [SerializeField] private AnimationCurve targetRpmPerFuel;
        [SerializeField] private AnimationCurve fuelPerThrottle;
        [SerializeField] private float torqueMul = 1;
        [SerializeField] private AnimationCurve torquePerDeltaRpm;
        [SerializeField] private float fuelConsumptionMul = 1;
        [SerializeField] private AnimationCurve viscousForcePerPower;
        [SerializeField] private float viscousForceMul;
        [SerializeField] private AnimationCurve relaxationRatePerPower;
        [SerializeField] private AnimationCurve relaxationRatePerForce;
        [SerializeField] private float relaxationRateMul;
        [SerializeField] private AnimationCurve stiffnessPerPower; 
        [SerializeField] private float stiffnessMul = 50.0f; 
        [SerializeField] private float screwPitchDeg = 10;
        [SerializeField] private float powerConsumption = 1;
        [SerializeField] private float rotorRadius = 1;

        [ShowInInspector, ReadOnly] private float _engineSpeedRadians;
        [ShowInInspector, ReadOnly] private float _consumedFuel;
        private float _massInv;
        private DynamicStructure _structure;
        [ShowInInspector] private MediumState _mediumState;
        private float _powerValue;
        private float _maxRadiansPerSec;
        private float _maxRadiansPerSecInv;
        private float _rotorInertiaInv;

        public Port<float> throttle = new Port<float>(PortType.Thrust);
        public Port<float> screwPitchHandle = new Port<float>(PortType.Thrust);
        public Port<float> powerHandle = new Port<float>(PortType.Signal);
        public StoragePort fuel = new StoragePort(typeof(Hydrogen));
        public Port<float> rpm = new Port<float>(PortType.Signal);
        private float _relaxRate;
        private float _normalForceSingle;

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
            _structure = (DynamicStructure)structure;
            OnValidatePrivate();
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            OnValidatePrivate();
        }
        
        private void OnValidatePrivate()
        {
            _massInv = 1f / rotorMass;
            _maxRadiansPerSec = maxRpm * Mathf.PI / 30;
            _maxRadiansPerSecInv = 1f / _maxRadiansPerSec;
            _rotorInertiaInv = 1f / (rotorMass * rotorRadius * rotorRadius);
        }
        
        public void UpdateBlock()
        {
            rpm.Value = _engineSpeedRadians / Mathf.PI * 30;
        }

        public void FuelTick()
        {
            float amount = Mathf.Clamp(fuelPerThrottle.Evaluate(throttle.Value) * fuelConsumptionMul, 0f, fuel.Value);
            _consumedFuel = amount;
            fuel.Value -= amount.DeltaTime();
            float targetEngineSpeed = targetRpmPerFuel.Evaluate(_consumedFuel) * _maxRadiansPerSec;
            float deltaEngineSpeed = targetEngineSpeed - _engineSpeedRadians;
            float torque = torquePerDeltaRpm.Evaluate(deltaEngineSpeed * _maxRadiansPerSecInv) * torqueMul;
            _engineSpeedRadians += torque.DeltaTime() * _massInv;
        }

        public void ApplyForce()
        {
            CalculateViscoElasticForces(ref _mediumState, transform, _structure.Velocity, _engineSpeedRadians, screwPitchDeg * screwPitchHandle.Value * Mathf.Deg2Rad, rotorRadius, _rotorInertiaInv, 1f.DeltaTime(), out var output);
            _structure.AddForce(output.force, transform.position);
            _structure.AddTorque(output.torque);
            _engineSpeedRadians += output.angularAcceleration * Time.deltaTime;
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

            // 5. Get reaction force from BOTH instantaneous speed AND accumulated compression
            float normalForceSingle = GetReactionForce(speedAlongNormal, state.accumulatedNormalStrain);
            
            // 4. Medium relaxation (strain decays over time due to viscosity/flow)
            // Using exponential decay for frame-rate independent dissipation
            float relaxationRateByPower = relaxationRatePerPower.Evaluate(powerHandle.Value);
            _normalForceSingle = normalForceSingle;
            float relaxationRateByForce = relaxationRatePerForce.Evaluate(Mathf.Abs(_normalForceSingle / torqueMul));
            float prevStrain = state.accumulatedNormalStrain;
            state.accumulatedNormalStrain *= Mathf.Exp(-(relaxationRateByPower + relaxationRateByForce) * relaxationRateMul * deltaTime);
            _relaxRate = prevStrain - state.accumulatedNormalStrain;
            
            // 6. Decompose to axial thrust (Z) and rotational drag (tangential)
            float axialThrust = -_normalForceSingle * cosPitch;
            float tangentialForce = -_normalForceSingle * sinPitch;
            float axialTorque = tangentialForce * radius;

            // 7. World-space output vectors
            Vector3 forwardAxis = propellerTransform.forward;
            output.force = forwardAxis * axialThrust;
            output.torque = forwardAxis * axialTorque;

            // 8. Rotational acceleration (alpha = Tau / I)
            output.angularAcceleration = (rotorInertiaInv > 0.0001f) ? (axialTorque * rotorInertiaInv) : 0f;
        }
        
        /*private void OnGUI()
        {
            GUI.skin.label.fontSize = 26;
            GUILayout.BeginVertical();
            GUILayout.Space(100);
            GUILayout.Label($"Relaxation rate: {_relaxRate:0.000}");
            GUILayout.Space(40);
            GUILayout.Label($"Normal force: {Mathf.Abs(_normalForceSingle / torqueMul):0.00}");
            float energyKinetic = _structure.Velocity.sqrMagnitude * _structure.Mass * 0.5f;
            float energyPotential = _structure.Mass * 9.81f * _structure.transform.position.y;
            GUILayout.Label($"Energy: {energyKinetic + energyPotential:N}. Kinetic: {energyKinetic:N}. Potential: {energyPotential:N}");
            GUILayout.EndVertical();
        }*/

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