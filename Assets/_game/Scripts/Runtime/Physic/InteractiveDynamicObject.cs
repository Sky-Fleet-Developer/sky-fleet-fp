using Core.Character.Interaction;
using Core.Structure.Rigging;
using Runtime.Items;
using UnityEngine;

namespace Runtime.Physic
{
    public class InteractiveDynamicObject : MonoBehaviour, IDragAndDropObjectHandler, IInteractiveObject
    {
        [SerializeField] private float disruptionTension = 2;
        [SerializeField] private bool moveTransitional;
        private Rigidbody _rigidbody; //rigidbody can be destroyed on containers attached to vehicle

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (TryGetComponent(out ItemObject itemObject))
            {
                itemObject.OnItemInitialized.Subscribe(() => _rigidbody.mass = itemObject.SourceItem.GetMass());
            }
        }
        
        public void Interact(InteractEventData data)
        {
            if (data.used || data.KeyModifier == KeyModifier.Down || data.MouseDelta.magnitude < 10)
            {
                return;
            }
            data.Controller.EnterHandler(this);
            data.Use();
        }

        public Rigidbody Rigidbody
        {
            get
            {
                if (!_rigidbody) _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }
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