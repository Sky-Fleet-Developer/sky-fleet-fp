using System;
using UnityEngine;

namespace Runtime.Physic
{
    public class RopeHook : MonoBehaviour
    {
        [SerializeField] private Rope rope;
        [SerializeField] private Vector3 connectedAnchor;
        private bool _connected = false;
        private void Awake()
        {
            rope.OnInitialize.Subscribe(() =>
            {
                rope.Connect(GetComponent<Rigidbody>(), connectedAnchor);
            });
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_connected)
            {
                return;
            }
            var anchor = other.GetComponent<RopeHookAnchor>();
            if (anchor)
            {
                _connected = true;
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
    }
}