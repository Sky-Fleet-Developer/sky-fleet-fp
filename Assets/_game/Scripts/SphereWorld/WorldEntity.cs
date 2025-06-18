using System;
using Core.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SphereWorld
{
    public class WorldEntity : MonoBehaviour
    {
        [SerializeField] private Polar polar;
        private Space _space;
        private bool _isVisible;
        public event Action OnEntityBecameVisible;
        public event Action OnEntityBecameInvisible;
        public bool IsVisible => _isVisible;
        private CachedRequest<Vector3> _offsetRequest;
        
        public Polar GetPolar() => polar;
        public void SetPolar(Polar value) => polar = value;

        private void Awake()
        {
            _offsetRequest = new(Time.fixedDeltaTime * 3, () => _space.GetOffset(polar));
        }

        public void InjectSpace(Space space)
        {
            _space = space;
            if (_space.IsVisible(polar))
            {
                OnVisible();
            }
        }

        public Vector3 GetOffset()
        {
            return _offsetRequest.Value;
        }

        private void OnVisible()
        {
            if (!_isVisible)
            {
                _isVisible = true;
            }
            OnEntityBecameVisible?.Invoke();
        }
    }
}
