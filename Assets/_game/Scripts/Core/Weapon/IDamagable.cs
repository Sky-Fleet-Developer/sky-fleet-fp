using System.Collections.Generic;

namespace Core.Weapon
{
    public interface IDamageModifier
    {
        float Apply(float f);
    }
    
    public interface IDamagable
    {
        public void Hit(IDamageSource damageSource, HitData data, IEnumerable<IDamageModifier> modifiers);
    }
}