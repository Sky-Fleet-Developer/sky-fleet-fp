using UnityEngine;

namespace Runtime.Physic
{
    public class RopeHookAnchor : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        public Rigidbody Body => _rigidbody;
        
        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
        }

        public Vector3 GetConnectedAnchor()
        {
            return _rigidbody.transform.InverseTransformPoint(transform.position);
        }
    }
}