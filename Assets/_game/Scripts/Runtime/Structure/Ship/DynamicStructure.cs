using Core.Structure;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Ship
{
    [RequireComponent(typeof(Rigidbody))]
    public class DynamicStructure : BaseStructure, IDynamicStructure
    {
        public float Mass => rigidbody.mass;
        
        public Vector3 Velocity => rigidbody.velocity;

        [ShowInInspector]
        public float TotalWeight { get; private set; }

        [ShowInInspector]
        public Vector3 LocalCenterOfMass { get; private set; }

        public Rigidbody Physics => rigidbody;

        private Rigidbody rigidbody;

        public override void Init()
        {
            rigidbody = GetComponent<Rigidbody>();
            base.Init();
        }

        protected override void OnFinishInit()
        {
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
            TotalWeight = mass + Mass;
            LocalCenterOfMass = pos / (mass + Mass);
            rigidbody.mass = TotalWeight;
            rigidbody.centerOfMass = LocalCenterOfMass;
        }
    }
}