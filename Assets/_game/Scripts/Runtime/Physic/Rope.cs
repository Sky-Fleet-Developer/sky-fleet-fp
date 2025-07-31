using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Game;
using Core.Utilities;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Runtime.Physic
{
    [DrawWithUnity]
    public class Rope : MonoBehaviour
    {
        [SerializeField, HideInInspector] private float length;
        [SerializeField] private Rigidbody connectedBody;

        [ShowInInspector]
        public float Length
        {
            get => length;
            set
            {
                length = value;
                var joint = _mainJoint ?? _hook;
                var limit = joint.linearLimit;
                limit.limit = value;
                joint.linearLimit = limit;
            }
        }
        private ConfigurableJoint _mainJoint;
        private ConfigurableJoint _hook;
        public LateEvent OnInitialize = new LateEvent();
        public event Action OnDetached;
        public bool IsConnected => _mainJoint != null;


        private void Awake()
        {
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                bool isKinematic = connectedBody.isKinematic;
                
                connectedBody.isKinematic = isKinematic;
                OnInitialize.Invoke();
            });
        }
        
        public Vector3 GetHookPoint()
        {
            var joint = _mainJoint ?? _hook;
            return transform.InverseTransformPoint(joint.transform.TransformPoint(joint.anchor));
        }

        public float GetDistance()
        {
            var joint = _mainJoint ?? _hook;
            return Vector3.Distance(joint.transform.TransformPoint(joint.anchor), transform.position);
        }

        public void Connect(Rigidbody target, Vector3 connectedAnchor)
        {
            _mainJoint = ConnectPrivate(target, connectedAnchor, out length);
            if (_hook)
            {
                _hook.connectedBody = target;
                _hook.connectedAnchor = _mainJoint.anchor;
                var limit = _hook.linearLimit;
                limit.limit = 0;
                _hook.linearLimit = limit;
            }
        }

        public void ConnectAsHook(Rigidbody target, Vector3 connectedAnchor)
        {
            _hook = ConnectPrivate(target, connectedAnchor, out length);
        }

        public void Detach()
        {
            _hook.connectedBody = connectedBody;
            _hook.connectedAnchor = connectedBody.transform.InverseTransformPoint(transform.position);
            var limit = _hook.linearLimit;
            limit.limit = length;
            _hook.linearLimit = limit;
            Destroy(_mainJoint);
            _mainJoint = null;
            OnDetached?.Invoke();
        }
        
        private ConfigurableJoint ConnectPrivate(Rigidbody target, Vector3 connectedAnchor, out float length)
        {
            var joint = target.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(transform.position);
            joint.anchor = connectedAnchor;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.enableCollision = true;
            var limit = joint.linearLimit;
            length = Vector3.Distance(target.transform.TransformPoint(connectedAnchor), transform.position);
            limit.limit = length;
            joint.linearLimit = limit;
            return joint;
        }
    }
}
