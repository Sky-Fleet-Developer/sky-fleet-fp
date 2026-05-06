using System.Collections.Generic;
using UnityEngine;

namespace Core.Weapon
{
    public class DamageProvider : MonoBehaviour, IDamagable
    {
        private IDamagable _parent;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Damagable");
        }
        
        private void Awake()
        {
            _parent = transform.parent.GetComponentInParent<IDamagable>();
        }

        public void Hit(IDamageSource damageSource, HitData data, IEnumerable<IDamageModifier> modifiers)
        {
            _parent.Hit(damageSource, data, modifiers);
        }
    }
}