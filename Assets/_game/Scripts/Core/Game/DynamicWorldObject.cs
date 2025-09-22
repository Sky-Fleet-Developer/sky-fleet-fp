using System;
using UnityEngine;

namespace Core.Game
{
    public interface IMassCombinator
    {
        public void SetMassDirty(IMassModifier massModifier);
    }
    public interface IMassModifier
    {
        float Mass { get; }
        void AddListener(IMassCombinator massCombinator);
        void RemoveListener(IMassCombinator massCombinator);
    }
    public class DynamicWorldObject : MonoBehaviour, IMassCombinator
    {
        private Rigidbody[] _rigidbodies;
        private float[] _masses;
        private Vector4 _massCache;
        private bool _isStatic;
        private IMassModifier[] _massModifiers;
        private float _initialTotalMass;

        private void Awake()
        {
            _isStatic = false;
            _rigidbodies = GetComponentsInChildren<Rigidbody>();
            bool[] kinematicFlags = new bool[_rigidbodies.Length];
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                kinematicFlags[i] = _rigidbodies[i].isKinematic;
                _rigidbodies[i].isKinematic = true;
                _rigidbodies[i].AddWorldOffsetAnchor();
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
            Vector3 center = Vector3.zero;

            float totalMass = _initialTotalMass;
            foreach (var massModifier in _massModifiers)
            {
                totalMass += massModifier.Mass;
            }

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

        public void SetMassDirty(IMassModifier _)
        {
            CacheMass();
        }
    }
}