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
    }
}