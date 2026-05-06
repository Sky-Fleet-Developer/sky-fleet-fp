using System.Collections.Generic;
using Core.Weapon;
using UniRx;
using UnityEngine;

namespace Runtime.Damage
{
    public class ReverseHealth : MonoBehaviour, IDamagable
    {
        private FloatReactiveProperty _damage = new ();
        public IReadOnlyReactiveProperty<float> Damage => _damage;
        
        public void Hit(IDamageSource damageSource, HitData data, IEnumerable<IDamageModifier> modifiers)
        {
            _damage.Value += damageSource.Impulse * damageSource.Size;
        }
    }
}