using System;
using Core.Game;
using NWH.WheelController3D;
using UnityEngine;

namespace Runtime.Physic
{
    public class WheelWorldOffsetAdaptor : MonoBehaviour
    {
        [SerializeField] private WheelController wheel;

        private void Awake()
        {
            WorldOffset.OnWorldOffsetChange += OffsetChanged;
        }

        private void OnDestroy()
        {
            WorldOffset.OnWorldOffsetChange -= OffsetChanged;
        }

        private void OffsetChanged(Vector3 offset)
        {
            wheel.CorrectReferencePosition(offset);
        }
    }
}