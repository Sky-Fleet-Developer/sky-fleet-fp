using System;
using UnityEngine;

namespace Core.Game
{
    public class DynamicWorldObject : MonoBehaviour, IMassCombinator
    {
        private Rigidbody[] _rigidbodies;
        private GameObject[] _rigidbodyOwners;
        private float[] _masses;
        private Vector4 _massCache;
        private bool _isStatic;
        private IMassModifier[] _massModifiers;
        private float _initialTotalMass;
        public event Action OnMassChanged;

        private void Awake()
        {
            _isStatic = false;
            _rigidbodies = GetComponentsInChildren<Rigidbody>();
            _rigidbodyOwners = new GameObject[_rigidbodies.Length];
            bool[] kinematicFlags = new bool[_rigidbodies.Length];
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                kinematicFlags[i] = _rigidbodies[i].isKinematic;
                _rigidbodies[i].isKinematic = true;
                _rigidbodies[i].AddWorldOffsetAnchor();
                _rigidbodyOwners[i] = _rigidbodies[i].gameObject;
            }

            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                for (var i = 0; i < _rigidbodies.Length; i++)
                {
                    _rigidbodies[i].isKinematic = kinematicFlags[i];
                }
            });
        }

        private void Start()
        {
            _masses = new float[_rigidbodies.Length];
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                _masses[i] = _rigidbodies[i].mass;
                _initialTotalMass += _masses[i];
            }
            
            _massModifiers = GetComponents<IMassModifier>();
            foreach (var massModifier in _massModifiers)
            {
                massModifier.AddListener(this);
            }

            CacheMass();
        }

        public void ConvertToStatic()
        {
            _isStatic = true;
            CacheMass();
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                _rigidbodies[i].RemoveWorldOffsetAnchor();
                Destroy(_rigidbodies[i]);
            }
        }
        
        public void ConvertToDynamic()
        {
            _isStatic = false;
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                _rigidbodies[i] = _rigidbodyOwners[i].AddComponent<Rigidbody>();
                _rigidbodies[i].mass = _masses[i];
            }
            CacheMass();
        }

        public Vector4 GetMass()
        {
            if (!_isStatic)
            {
                CacheMass();
            }

            return _massCache;
        }

        private void CacheMass()
        {
            float totalMass = _initialTotalMass;
            foreach (var massModifier in _massModifiers)
            {
                totalMass += massModifier.Mass;
            }
            if (_isStatic)
            {
                _massCache.w = totalMass;
            }
            else
            {
                Vector3 center = Vector3.zero;
                float mul = totalMass / _initialTotalMass;
                for (var i = 0; i < _rigidbodies.Length; i++)
                {
                    float partMass = _masses[i] * mul;
                    _rigidbodies[i].mass = partMass;
                    center += _rigidbodies[i].transform.TransformPoint(_rigidbodies[i].centerOfMass) * partMass;
                }
                center = transform.InverseTransformPoint(center / totalMass);
                _massCache = new Vector4(center.x, center.y, center.z, totalMass);
            }
            OnMassChanged?.Invoke();
        }

        public void SetMassDirty(IMassModifier _)
        {
            CacheMass();
        }
    }
}