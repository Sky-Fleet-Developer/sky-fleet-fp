using UnityEngine;

namespace Core.Weapon
{
    public interface IKineticWeapon
    {
        public Transform Muzzle { get; }
        public float Energy { get; }
        public float SpreadFactor { get; }
    }
}