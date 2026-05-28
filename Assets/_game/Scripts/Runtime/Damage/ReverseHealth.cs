using System.Collections.Generic;
using Core.Structure.Damage;
using Core.Weapon;
using UniRx;
using UnityEngine;

namespace Runtime.Damage
{
    public class ReverseHealth : MonoBehaviour, IDamagable, IInitAsDamageModel
    {
        private FloatReactiveProperty _damage = new ();
        public IReadOnlyReactiveProperty<float> Damage => _damage;
        
        public void Hit(IDamageSource damageSource, HitData data, IEnumerable<IDamageModifier> modifiers)
        {
            _damage.Value += damageSource.Impulse * damageSource.Size;
            Debug.Log($"ReverseHealth damage: {_damage.Value}");
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