using System;
using Core.Character;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Physic
{
    public class InteractiveDynamicObject : MonoBehaviour, IInteractiveDynamicObject
    {
        [SerializeField] private float disruptionTension = 2;
        [SerializeField] private bool moveTransitional;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public bool EnableInteraction => gameObject.activeInHierarchy && enabled;
        public Transform Root => transform;
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }

        public Rigidbody Rigidbody => _rigidbody;
        public bool MoveTransitional => moveTransitional;
        public void StartPull()
        {
        }

        public bool ProcessPull(float tension)
        {
            return tension < disruptionTension;
        }
    }
}