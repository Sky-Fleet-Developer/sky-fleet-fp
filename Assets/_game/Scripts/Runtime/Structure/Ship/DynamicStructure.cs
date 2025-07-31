using Core.Structure;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Ship
{
    [RequireComponent(typeof(Rigidbody))]
    public class DynamicStructure : BaseStructure, IDynamicStructure
    {
        [SerializeField] private float emptyMass;
        public float Mass => emptyMass;
        
        public Vector3 Velocity => rigidbody.velocity;

        [ShowInInspector, ReadOnly]
        public float TotalMass { get; private set; }

        [ShowInInspector, ReadOnly]
        public Vector3 LocalCenterOfMass { get; private set; }

        public Rigidbody Physics => rigidbody;

        private Rigidbody rigidbody;

        public override void Init(bool force)
        {
            rigidbody = GetComponent<Rigidbody>();
            base.Init(force);
            RecalculateMass();
        }

        public Vector3 GetVelocityForPoint(Vector3 worldPoint)
        {
            return rigidbody.GetPointVelocity(worldPoint);
        }

        public void AddForce(Vector3 force, Vector3 position)
        {
            rigidbody.AddForceAtPosition(force, position);
        }

        public void RecalculateMass()
        {
            float mass = 0;
            Vector3 pos = Vector3.zero;
            Blocks.ForEach(x =>
            {
                mass += x.Mass;
                pos += x.transform.localPosition * x.Mass;
            });
            TotalMass = mass + Mass;
            LocalCenterOfMass = pos / (mass + Mass);
            rigidbody.mass = TotalMass;
            rigidbody.centerOfMass = LocalCenterOfMass;
        }
    }
}