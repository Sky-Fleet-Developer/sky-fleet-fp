using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Structure.Rigging.Power;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class JointRotator : PowerUserBlock, IUpdatableBlock
    {
        [SerializeField] private HingeJoint joint;
        [SerializeField] private float speedMul = 1;
        [SerializeField] private float maxTorque;
        [SerializeField] private float torqueMul;
        [SerializeField] private float tensionMul;
        [SerializeField] private float maxTension;
        [SerializeField] private float idleTorquePercent = 0.3f;
        [SerializeField] private AnimationCurve consumptionPerTorque;
        [SerializeField] private Vector3 localTestAxis;
        [SerializeField] private Vector3 connectedTestAxis;
        private Port<float> targetSpeed = new Port<float>(PortType.Signal);
        private Transform _bodyA;
        private Transform _bodyB;
        [ShowInInspector] private float _input;
        [ShowInInspector] private float _targetAngle;
        [ShowInInspector] private float _currentAngle;
        [ShowInInspector] private float _currentDelta;
        
        public override float Consumption
        {
            get
            {
                _input = targetSpeed.GetValue();
                float torque = Mathf.Min(Mathf.Abs(_input * torqueMul), maxTorque);
                return consumptionPerTorque.Evaluate(torque / maxTorque);
            }
        }
        
        private void Awake()
        {
            bool isJointSameObject = joint.gameObject == gameObject;
            _bodyA = joint.transform;
            _bodyB = isJointSameObject ? joint.connectedBody.transform : transform;
            _targetAngle = _currentAngle = GetCurrentAngle();
        }

        private float GetCurrentAngle()
        {
            Vector3 aLocalToA = localTestAxis;
            Vector3 globalB = _bodyB.transform.TransformDirection(connectedTestAxis);
            Vector3 bLocalToA = _bodyA.transform.InverseTransformDirection(globalB);
            return Vector3.SignedAngle(bLocalToA, aLocalToA, joint.axis);
        }

        public void UpdateBlock(int lod)
        {
            if (IsWork)
            {
                _targetAngle += _input.DeltaTime() * speedMul;
                _currentAngle = GetCurrentAngle();
                _currentDelta = _targetAngle - _currentAngle;
                if (_currentDelta > 180)
                {
                    _currentDelta -= 360;
                }
                else if (_currentDelta < -180)
                {
                    _currentDelta += 360;
                }
                _currentDelta = Mathf.Clamp(_currentDelta, -maxTension, maxTension);
                _targetAngle = _currentAngle + _currentDelta;
                var motor = joint.motor;
                motor.targetVelocity = _currentDelta * tensionMul;
                motor.force = (idleTorquePercent + (1 - idleTorquePercent) * Mathf.Abs(_input)) * maxTorque;
                joint.motor = motor;
            }
            else
            {
                var motor = joint.motor;
                motor.targetVelocity = 0;
                motor.force = maxTorque * idleTorquePercent;
                joint.motor = motor;
            }
        }
    }
}