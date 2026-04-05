using System;
using UnityEngine;

namespace Core.Weapon
{
    [Serializable]
    public class ProjectileSettings
    {
        public LayerMask layerMask;
        public float maxLifetime = 10f;
    }
}