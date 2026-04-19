using System;
using Core.Graph;
using Core.Graph.Wires;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using NWH.Common.Utility;
using Runtime.Physic;
using Runtime.Structure.Rigging.Power;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.Structure.Rigging.Control
{
    public abstract class AbstractRotator : PowerUserBlock, IUpdatableBlock
    {
        [SerializeField] private bool DebugEnabled;
        [SerializeField] private PortType controlPortType;
        private Port<float> targetAngle = new (PortType.Signal);
        private Port<float> currentAngle = new (PortType.Signal);
        private Port<float> velocity = new (PortType.Signal);
        [SerializeField] private bool isCycled;
        [SerializeField] private float rotationForce;
        [SerializeField] private float dragForce;
        [SerializeField] private float inputSignalMultiplier = 1;
        [SerializeField] private AnimationCurve consumptionPerPower;
        [SerializeField, BlockRelativeValue, HideIf("isCycled")] public Vector2 minMaxAngle;
        [PlayerProperty] public float InputSignalMultiplier
        {
            get => inputSignalMultiplier;
            set => inputSignalMultiplier = value;
        }
        [ShowInInspector, ReadOnly] private float _inertiaInv;
        [ShowInInspector, ReadOnly] private float _velocity;
        [ShowInInspector, ReadOnly] private float _currentAngle;
        [ShowInInspector, ReadOnly] private float _availablePower;
        [ShowInInspector, ReadOnly] private float _power;
        
        [SerializeField] private float maxConsumption;
        public override float Consumption => (IsWork ? 0.001f : 0) + consumptionPerPower.Evaluate(Mathf.Abs(_power)) * maxConsumption;
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            structure.OnInitComplete.Subscribe(OnInitComplete);
        }

        public override void InitNode(IGraph graph)
        {
            if (targetAngle == null || targetAngle.ValueType != controlPortType)
            {
                targetAngle = new(controlPortType);
            }
            base.InitNode(graph);
        }

        private void OnInitComplete()
        {
            var bounds = Parent.Bounds;
            _inertiaInv = PhysicsUtilities.GetAngularAcceleration(bounds.size, Parent.Mass, GetRotationAxis()).magnitude;
        }

        protected abstract Vector3 GetRotationAxis();

        public override void ConsumptionTick()
        {
            float target = targetAngle.GetValue() * inputSignalMultiplier;
            if (!isCycled)
            {
                target = Mathf.Clamp(target, minMaxAngle.x, minMaxAngle.y);
            }
            
            float vel = _velocity;
            Mathf.SmoothDampAngle(_currentAngle, target, ref vel, 1f / (rotationForce * _inertiaInv), Mathf.Infinity, CycleService.DeltaTime);
            _power = Mathf.Abs(_velocity - vel);
            
            base.ConsumptionTick();
        }

        public override void PowerTick()
        {
            if (Consumption == 0)
            {
                _availablePower = 0;
            }
            else
            {
                _availablePower = Power.charge / Consumption.DeltaTime();
            }
            base.PowerTick();
        }
       
        public void UpdateBlock()
        {
            if (IsWork)
            {
                Accelerate();
            }
            else
            {
                Decelerate();
            }

            if (_velocity != 0)
            {
                Rotate();
            }
            currentAngle.SetValue(_currentAngle);
            velocity.SetValue(_velocity);
        }

        private void Accelerate()
        {
            float target = targetAngle.GetValue() * inputSignalMultiplier;
            if (!isCycled)
            {
                target = Mathf.Clamp(target, minMaxAngle.x, minMaxAngle.y);
            }
            
            _currentAngle = Mathf.SmoothDampAngle(_currentAngle, target, ref _velocity, 1f / (rotationForce * _availablePower * _inertiaInv), Mathf.Infinity, CycleService.DeltaTime);
        }

        private void Decelerate()
        {
            _velocity -= Mathf.Min(Mathf.Abs(_velocity), dragForce * _inertiaInv) * Mathf.Sign(_velocity) * CycleService.DeltaTime;
            _currentAngle += _velocity * CycleService.DeltaTime;
            if (!isCycled)
            {
                if (_currentAngle < minMaxAngle.x)
                {
                    _velocity = Mathf.Max(0, _velocity);
                    _currentAngle = minMaxAngle.x;
                }
                if(_currentAngle > minMaxAngle.y)
                {
                    _velocity = Mathf.Min(0, _velocity);
                    _currentAngle = minMaxAngle.y;
                }
            }
        }
        
        private void Rotate()
        {
            _currentAngle = TryApplyRotation(_currentAngle);
        }

        protected abstract float TryApplyRotation(float angle);
    }
}