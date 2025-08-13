using System.Collections.Generic;
using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Ship
{
    [RequireComponent(typeof(Rigidbody))]
    public class DynamicStructure : Structure, IDynamicStructure
    {
        [SerializeField] private float emptyMass;
        [SerializeField] private Vector3 localCenterOfMass;
        public float Mass => emptyMass;
        
        public Vector3 Velocity => rigidbody.velocity;

        [ShowInInspector, ReadOnly]
        public float TotalMass { get; private set; }

        [ShowInInspector]
        public Vector3 LocalCenterOfMass { get; private set; }

        public Rigidbody Physics => rigidbody;

        private Rigidbody rigidbody;

        private List<IMass> _registeredMass = new ();
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
            float totalMass = Mass;
            Vector3 pos = transform.TransformPoint(localCenterOfMass) * Mass;
            Blocks.ForEach(x =>
            {
                totalMass += x.Mass;
                pos += x.transform.TransformPoint(x.LocalCenterOfMass) * x.Mass;
            });

            TotalMass = totalMass;
            LocalCenterOfMass = transform.InverseTransformPoint(pos / totalMass);
            rigidbody.mass = TotalMass;
            rigidbody.centerOfMass = LocalCenterOfMass;
        }
    }
}