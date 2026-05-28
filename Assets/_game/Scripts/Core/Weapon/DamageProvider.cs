using System.Collections.Generic;
using Core.Structure.Damage;
using UnityEngine;
using Zenject;

namespace Core.Weapon
{
    public class DamageProvider : MonoBehaviour, IDamagable, IInitAsDamageModel
    {
        [Inject] private ProjectileHandler _projectileHandler;
        private IDamagable _parent;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Damagable");
        }
        
        private void Awake()
        {
            _parent = transform.parent.GetComponentInParent<IDamagable>();
        }
        
        public void InitDamageModel()
        {
            if (gameObject.TryGetComponent(out Collider collider))
            {
                _projectileHandler.RegisterDamagable(collider.GetInstanceID(), this);
            }
        }

        public void Hit(IDamageSource damageSource, HitData data, IEnumerable<IDamageModifier> modifiers)
        {
            _parent.Hit(damageSource, data, modifiers);
        }

        public void InitAsDamageModel(ProjectileHandler projectileHandler)
        {
            enabled = false;
        }

        public void InitAsStructurePart(ProjectileHandler projectileHandler)
        {
            if (gameObject.TryGetComponent(out Collider collider))
            {
                gameObject.layer = LayerMask.NameToLayer("Damagable");
                projectileHandler.RegisterDamagable(collider.GetInstanceID(), this);
            }
        }
    }
}