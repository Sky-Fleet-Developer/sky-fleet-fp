using System.Collections.Generic;
using UnityEngine;

namespace Core.Weapon
{
    public class DamageProvider : MonoBehaviour, IDamagable
    {
        private IDamagable _parent;

        private void Awake()
        {
            _parent = transform.parent.GetComponentInParent<IDamagable>();
        }

        public void Hit(ProjectileInstance projectile, Vector3 hitPoint, Vector3 hitNormal, IEnumerable<IDamageModifier> modifiers)
        {
            _parent.Hit(projectile, hitPoint, hitNormal, modifiers);
        }
    }
}