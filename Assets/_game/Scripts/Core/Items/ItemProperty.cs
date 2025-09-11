using System;

namespace Core.Items
{
    [Serializable]
    public struct ItemProperty
    {
        public const int Resizable_MassByrLiter = 0;
        public const int Resizable_StackSize = 1;
        public const int Mass_MassByOne = 0;
        public const int Mass_VolumeByOne = 2;
        public const int Mass_StackSize = 2;
        public const int Liquid_MassByLiter = 0;
        public string name;
        public ItemPropertyValue[] values;
    }
    
    [Serializable]
    public struct ItemPropertyValue
    {
        public string stringValue;
        public int intValue;
        public float floatValue;
    }
}