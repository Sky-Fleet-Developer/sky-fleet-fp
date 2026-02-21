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
        
        public Vector3 Velocity => rigidbody.linearVelocity;

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
            /*Vector3 f = force * 0.0003f;
            Color c = Color.HSVToRGB(Mathf.Repeat(Time.time * 3f, 1f), 1f, 1f);
            Debug.DrawRay(position, f, c, 1);
            Vector3 cross = Vector3.Cross(f, transform.right).normalized;
            Debug.DrawRay(position + f, -f * 0.1f + cross * (f.magnitude * 0.1f), c, 1);
            Debug.DrawRay(position + f, -f * 0.1f - cross * (f.magnitude * 0.1f), c, 1);*/
        }

        public Vector3 GetPointVelocity(Vector3 transformPoint)
        {
            return rigidbody.GetPointVelocity(transformPoint);
        }

        public void RecalculateMass()
        {
            float totalMass = Mass;
            Vector3 pos = transform.TransformPoint(localCenterOfMass) * Mass;
            foreach (var block in Blocks)
            {
                totalMass += block.Mass;
                pos += block.transform.TransformPoint(block.LocalCenterOfMass) * block.Mass;
            }
            TotalMass = totalMass;
            LocalCenterOfMass = transform.InverseTransformPoint(pos / totalMass);
            rigidbody.mass = TotalMass;
            rigidbody.centerOfMass = LocalCenterOfMass;
        }
    }
}