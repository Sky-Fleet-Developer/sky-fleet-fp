using System;

namespace Core.Weapon
{
    public enum ChargeType
    {
        Ap = 0,
        He = 4,
        Aphe = 8,
        Droplet = 12
    }
    
    [Serializable]
    public struct ShellData
    {
        public CaliberSign caliber;
        public ChargeType chargeType;
        public float airDrag;
        public float mass;
    }
}