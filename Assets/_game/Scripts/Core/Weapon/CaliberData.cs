using System;

namespace Core.Weapon
{
    [Serializable]
    public struct ShellData
    {
        public CaliberSign caliber;
        public string chargeType;
        public float airDrag;
    }
}