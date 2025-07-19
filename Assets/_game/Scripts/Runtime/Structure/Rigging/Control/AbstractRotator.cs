using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Runtime.Physic;
using Runtime.Structure.Rigging.Power;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public abstract class AbstractRotator : PowerUserBlock, IUpdatableBlock
    {
        private Port<float> targetAngle = new Port<float>(PortType.Signal);
        private Port<float> currentAngle = new Port<float>(PortType.Signal);
        private Port<float> velocity = new Port<float>(PortType.Signal);
        [SerializeField] private bool isCycled;
        [SerializeField] private float rotationForce;
        [SerializeField] private float dragForce;
        [SerializeField] private AnimationCurve consumptionPerDelta;
        [SerializeField, ConstantField, HideIf("isCycled")] private Vector2 minMaxAngle;

        [ShowInInspector, ReadOnly] private float _inertia;
        [ShowInInspector, ReadOnly] private float _velocity;
        [ShowInInspector, ReadOnly] private float _currentAngle;
        [ShowInInspector, ReadOnly] private float _availablePower;
        private float _acceleration;
        
        [SerializeField] private float maxConsumption;
        public override float Consumption => consumptionPerDelta.Evaluate(Mathf.Abs(_acceleration)) * maxConsumption;
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            structure.OnInitComplete.Subscribe(OnInitComplete);
        }

        private void OnInitComplete()
        {
            var bounds = Parent.Bounds;
            _inertia = PhysicsUtilities.GetAngularAcceleration(bounds.size, Parent.Mass, GetRotationAxis()).magnitude;
        }

        protected abstract Vector3 GetRotationAxis();


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
       
        public void UpdateBlock(int lod)
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
            float target = targetAngle.GetValue();
            if (!isCycled)
            {
                target = Mathf.Clamp(target, minMaxAngle.x, minMaxAngle.y);
            }

            _currentAngle %= 360;
            float delta = target - _currentAngle;
            if (delta > 180)
            {
                delta -= 360;
            }
            else if (delta < -180)
            {
                delta += 360;
            }

            float maxAcceleration = _inertia * rotationForce * _availablePower;
            float slowingTime = Mathf.Abs(_velocity / maxAcceleration);
            int slowingSign = (int)Mathf.Sign(-_velocity);
            int deltaSign =  (int)Mathf.Sign(delta);
            float sSlowing = _velocity * slowingTime + (maxAcceleration * slowingSign * slowingTime * slowingTime) * 0.5f;

            _acceleration = 0;
            if (slowingSign != deltaSign)
            {
                if (Mathf.Abs(sSlowing) > Mathf.Abs(delta))
                {
                    _acceleration = slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
                else
                {
                    _acceleration = deltaSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
            }
            else
            {
                _acceleration = slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
            }

            _velocity += _acceleration;
        }

        private void Decelerate()
        {
            _acceleration = 0;
            _velocity -= Mathf.Min(Mathf.Abs(_velocity), dragForce * _inertia) * Mathf.Sign(_velocity) * StructureUpdateModule.DeltaTime;
        }
        
        private void Rotate()
        {
            _currentAngle += _velocity * StructureUpdateModule.DeltaTime;
            _currentAngle = TryApplyRotation(_currentAngle);
        }

        protected abstract float TryApplyRotation(float angle);
    }
}