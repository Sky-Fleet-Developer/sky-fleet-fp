using System;
using UnityEngine;

namespace Core.Game
{
    public class DynamicWorldObject : MonoBehaviour
    {
        private Rigidbody[] _rigidbodies;
        private float[] _masses;
        private Vector4 _massCache;
        private bool _isStatic;
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

            _masses = new float[_rigidbodies.Length];
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                _masses[i] = _rigidbodies[i].mass;
            }
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
            float totalMass = 0;
            Vector3 center = Vector3.zero;

            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                center += _rigidbodies[i].transform.TransformPoint(_rigidbodies[i].centerOfMass) *
                          _rigidbodies[i].mass;
                totalMass += _rigidbodies[i].mass;
            }

            center = transform.InverseTransformPoint(center / totalMass);
            _massCache = new Vector4(center.x, center.y, center.z, totalMass);
        }
    }
}