﻿using System;
using Core.Character;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Physic
{
    public class RopeHook : MonoBehaviour, IInteractiveDynamicObject
    {
        [SerializeField] private Rope rope;
        [SerializeField] private Vector3 connectedAnchor;
        [SerializeField] private float maxPullTensionToDetach;
        private Rigidbody _rigidbody;
        private bool _connectionStateOnStartPull;
        private float _lastDetachTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            rope.OnInitialize.Subscribe(() =>
            {
                var rb = GetComponent<Rigidbody>();
                rope.ConnectAsHook(rb, connectedAnchor);
            }, 100);
            rope.OnDetached += OnDetached;
        }

        private void OnDetached()
        {
            _lastDetachTime = Time.time;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (rope.IsConnected || Time.time < _lastDetachTime + 1f)
            {
                return;
            }
            var anchor = other.GetComponent<RopeHookAnchor>();
            if (anchor)
            {
                rope.Connect(anchor.Body, anchor.GetConnectedAnchor());
            }
            /*var anchors = other.GetComponentsInChildren<RopeHookAnchor>();
            if (anchors.Length == 0)
            {
                return;
            }

            float distance = float.MaxValue;
            int index = -1;
            for (var i = 0; i < anchors.Length; i++)
            {
                float d = Vector3.SqrMagnitude(transform.position - anchors[i].transform.position);
                if (d < distance)
                {
                    distance = d;
                    index = i;
                }
            }

            if (index >= 0)
            {
                rope.Connect(anchors[index].Body, anchors[index].GetConnectedAnchor());
            }*/
        }

        public bool EnableInteraction => gameObject.activeInHierarchy && enabled;
        public Transform Root => transform;
        (bool canInteract, string data) IInteractiveObject.RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }
        Rigidbody IInteractiveDynamicObject.Rigidbody => _rigidbody;
        bool IInteractiveDynamicObject.MoveTransitional => true;
        void IInteractiveDynamicObject.StartPull()
        {
            _connectionStateOnStartPull = rope.IsConnected;
        }

        bool IInteractiveDynamicObject.ProcessPull(float tension)
        {
            if (rope.IsConnected)
            {
                if (tension > maxPullTensionToDetach)
                {
                    rope.Detach();
                }
            }
            else
            {
                return _connectionStateOnStartPull == rope.IsConnected;
            }
            return true;
        }
    }
}