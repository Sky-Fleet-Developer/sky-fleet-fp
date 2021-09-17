using UnityEngine;

namespace Structure.Ship
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShipTest : BaseStructure, IDynamicStructure
    {
        public float Mass => rigidbody.mass; //TODO: calculate mass from blocks count and self body mass
        public Vector3 Velocity => rigidbody.velocity;
        private Rigidbody rigidbody;
        
        protected override void Awake()
        {
            base.Awake();
            
            rigidbody = GetComponent<Rigidbody>();
        }

        public Vector3 GetVelocityForPoint(Vector3 worldPoint)
        {
            return rigidbody.GetPointVelocity(worldPoint);
        }

        public void AddForce(Vector3 force, Vector3 position)
        {
            rigidbody.AddForceAtPosition(force, position);
        }
    }
}