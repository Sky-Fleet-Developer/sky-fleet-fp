using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Runtime.Physic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities = Core.Structure.Rigging.Utilities;

namespace Runtime.Structure.Rigging.Control
{
    public class Rotator : BlockWithNode, IConsumer, IUpdatableBlock
    {
        [SerializeField] private PowerPort power = new PowerPort();
        private Port<float> targetAngle = new Port<float>(PortType.Signal);
        private Port<float> currentAngle = new Port<float>(PortType.Signal);
        private Port<float> velocity = new Port<float>(PortType.Signal);
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private bool isCycled;
        [SerializeField] private float rotationForce;
        [SerializeField] private float dragForce;
        [SerializeField, ConstantField] private string targetParent;
        [SerializeField, ConstantField, HideIf("isCycled")] private Vector2 minMaxAngle;

        private Transform _targetParent;
        [ShowInInspector]
        private Transform TargetParent
        {
            get
            {
                if (!_targetParent)
                {
                    var structure = Structure ?? GetComponentInParent<IStructure>();
                    if (structure == null)
                    {
                        return null;
                    }
                    var parent = structure.GetParentByPath(targetParent);
                    if (parent != null)
                    {
                        _targetParent = parent.Transform;
                    }
                }
                return _targetParent;
            }
            set
            {
                var structure = Structure ?? GetComponentInParent<IStructure>();
                if (structure == null)
                {
                    return;
                }
                if (!value.IsChildOf(structure.transform))
                {
                    return;
                }
                targetParent = value.GetPath(structure.transform);
                _targetParent = value;
            }
        }
        
        private Quaternion initialRotation;
        [ShowInInspector, ReadOnly] private float inertia;
        [ShowInInspector, ReadOnly] private float _velocity;
        [ShowInInspector, ReadOnly] private float _currentAngle;
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            initialRotation = TargetParent.localRotation;
            rotationAxis.Normalize();
            structure.OnInitComplete.Subscribe(OnInitComplete);
        }

        private void OnInitComplete()
        {
            var bounds = Parent.Bounds;
            inertia = PhysicsUtilities.GetAngularAcceleration(bounds.size, Parent.Mass, rotationAxis).magnitude;
        }

        public void ConsumptionTick()
        {
            Utilities.CalculateConsumerTickA(this);
        }

        public void PowerTick()
        {
            IsWork = Utilities.CalculateConsumerTickB(this);
        }
        public bool IsWork { get; private set; }
        [SerializeField] private float maxConsumption;
        public float Consumption => maxConsumption;
        public PowerPort Power => power;

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

            float maxAcceleration = inertia * rotationForce;
            float slowingTime = Mathf.Abs(_velocity / maxAcceleration);
            int slowingSign = (int)Mathf.Sign(-_velocity);
            int deltaSign =  (int)Mathf.Sign(delta);
            float sSlowing = _velocity * slowingTime + (maxAcceleration * slowingSign * slowingTime * slowingTime) * 0.5f;

            if (slowingSign != deltaSign)
            {
                if (Mathf.Abs(sSlowing) > Mathf.Abs(delta))
                {
                    _velocity += slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
                else
                {
                    _velocity += deltaSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
            }
            else
            {
                _velocity += slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
            }
        }

        private void Decelerate()
        {
            _velocity -= Mathf.Min(Mathf.Abs(_velocity), dragForce * inertia) * Mathf.Sign(_velocity) * StructureUpdateModule.DeltaTime;
        }
        
        private void Rotate()
        {
            _currentAngle += _velocity * StructureUpdateModule.DeltaTime;
            TargetParent.localRotation = initialRotation * Quaternion.AngleAxis(_currentAngle, rotationAxis);
        }
    }
}
