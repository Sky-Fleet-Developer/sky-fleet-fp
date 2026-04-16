using System;
using Sirenix.OdinInspector;

namespace Core.Weapon
{
    [Serializable]
    public struct KineticWeaponData
    {
        public CaliberSign caliber;
        public float spread;
        public float impulse;

#if UNITY_EDITOR
        [ShowInInspector, ReadOnly] private float startSpeedFor40GrammsCharce => impulse / 0.04f;
#endif
    }
}