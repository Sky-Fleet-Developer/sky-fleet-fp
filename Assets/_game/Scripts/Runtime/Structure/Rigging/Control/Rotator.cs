using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Physic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities = Core.Structure.Rigging.Utilities;

namespace Runtime.Structure.Rigging.Control
{
    public class Rotator : BlockWithNode, IConsumer, IUpdatableBlock
    {
        [SerializeField] private PowerPort power = new PowerPort();
        private Port<float> targetAngle = new Port<float>(PortType.Thrust);
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private bool isCycled;
        [SerializeField] private float rotationForce;
        [SerializeField] private float dragForce;
        [SerializeField, HideIf("isCycled")] private Vector2 minMaxAngle;
        private Quaternion initialRotation;
        [ShowInInspector, ReadOnly] private float inertia;
        [ShowInInspector, ReadOnly] private float velocity;
        [ShowInInspector, ReadOnly] private float currentAngle;
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            initialRotation = parent.Transform.localRotation;
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

            if (velocity != 0)
            {
                Rotate();
            }
        }

        private void Accelerate()
        {
            float target = targetAngle.GetValue() * 360;
            if (!isCycled)
            {
                target = Mathf.Clamp(target, minMaxAngle.x, minMaxAngle.y);
            }

            currentAngle %= 360;
            float delta = target - currentAngle;
            if (delta > 180)
            {
                delta -= 360;
            }
            else if (delta < -180)
            {
                delta += 360;
            }

            float maxAcceleration = inertia * rotationForce;
            float slowingTime = Mathf.Abs(velocity / maxAcceleration);
            int slowingSign = (int)Mathf.Sign(-velocity);
            int deltaSign =  (int)Mathf.Sign(delta);
            float sSlowing = velocity * slowingTime + (maxAcceleration * slowingSign * slowingTime * slowingTime) * 0.5f;

            if (slowingSign != deltaSign)
            {
                if (Mathf.Abs(sSlowing) > Mathf.Abs(delta))
                {
                    velocity += slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
                else
                {
                    velocity += deltaSign * maxAcceleration * StructureUpdateModule.DeltaTime;
                }
            }
            else
            {
                velocity += slowingSign * maxAcceleration * StructureUpdateModule.DeltaTime;
            }
        }

        private void Decelerate()
        {
            velocity -= Mathf.Min(Mathf.Abs(velocity), dragForce * inertia) * Mathf.Sign(velocity) * StructureUpdateModule.DeltaTime;
        }
        
        private void Rotate()
        {
            currentAngle += velocity * StructureUpdateModule.DeltaTime;
            Parent.Transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
        }
    }
}
