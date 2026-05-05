using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Weapon
{
    [Serializable]
    public class ProjectileSettings
    {
        [FormerlySerializedAs("layerMask")] public LayerMask regularLayerMask;
        public LayerMask structureHitsLayerMask;
        public float maxLifetime = 10f;
    }
}