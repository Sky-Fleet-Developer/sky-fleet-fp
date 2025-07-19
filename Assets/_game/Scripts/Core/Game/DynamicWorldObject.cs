using System;
using UnityEngine;

namespace Core.Game
{
    public class DynamicWorldObject : MonoBehaviour
    {
        private Rigidbody[] _rigidbodies;
        private void Awake()
        {
            _rigidbodies = GetComponentsInChildren<Rigidbody>();
            bool[] kinematicFlags = new bool[_rigidbodies.Length];
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                kinematicFlags[i] = _rigidbodies[i].isKinematic;
                _rigidbodies[i].isKinematic = true;
                if (!_rigidbodies[i].TryGetComponent(out WorldOffsetAnchor anchor))
                {
                    _rigidbodies[i].gameObject.AddComponent<WorldOffsetAnchor>();
                }
            }

            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                for (var i = 0; i < _rigidbodies.Length; i++)
                {
                    _rigidbodies[i].isKinematic = kinematicFlags[i];
                }
            });
        }
    }
}