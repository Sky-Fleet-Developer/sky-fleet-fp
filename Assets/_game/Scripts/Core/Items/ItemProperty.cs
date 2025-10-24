using System;
using System.Linq;

namespace Core.Items
{
    [Serializable]
    public struct ItemProperty : ICloneable
    {
        public const int Resizable_MassByrLiter = 0;
        public const int Resizable_StackSize = 1;
        public const int Mass_MassByOne = 0;
        public const int Mass_VolumeByOne = 1;
        public const int Mass_StackSize = 2;
        public const int Liquid_MassByLiter = 0;
        public const int Container_Volume = 0;
        public const int Container_IncludeRules = 0;
        public const int Container_ExcludeRules = 0;
        public const int IdentifiableInstance_Identifier = 0;
        public string name;
        public ItemPropertyValue[] values;
        public object Clone()
        {
            return new ItemProperty {name = name, values = values.ToArray()};
        }
    }
    
    [Serializable]
    public struct ItemPropertyValue
    {
        public string stringValue;
        public int intValue;
        public float floatValue;
    }
}