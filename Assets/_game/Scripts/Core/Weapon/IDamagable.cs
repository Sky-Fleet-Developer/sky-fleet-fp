using System.Collections.Generic;
using UnityEngine;

namespace Core.Weapon
{
    public interface IDamageModifier
    {
        float Apply(float f);
    }
    
    public interface IDamagable
    {
        public void Hit(ProjectileInstance projectile, Vector3 hitPoint, Vector3 hitNormal, IEnumerable<IDamageModifier> modifiers);
    }
}