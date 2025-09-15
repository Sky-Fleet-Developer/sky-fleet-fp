using Core.Character;
using Core.Structure.Rigging;
using Runtime.Items;
using UnityEngine;

namespace Runtime.Physic
{
    public class InteractiveDynamicObject : MonoBehaviour, IInteractiveDynamicObject
    {
        [SerializeField] private float disruptionTension = 2;
        [SerializeField] private bool moveTransitional;
        private Rigidbody _rigidbody;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (TryGetComponent(out ItemObject itemObject))
            {
                itemObject.OnItemInitialized.Subscribe(() => _rigidbody.mass = itemObject.SourceItem.GetMass());
            }
        }

        public bool EnableInteraction => gameObject.activeInHierarchy && enabled;
        public Transform Root => transform;
        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
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