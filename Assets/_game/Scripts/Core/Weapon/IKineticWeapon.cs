using Core.Items;
using UnityEngine;

namespace Core.Weapon
{
    public interface IKineticWeapon : IItemObject
    {
        public Transform Muzzle { get; }
        public Vector3 Velocity { get; }
    }
}