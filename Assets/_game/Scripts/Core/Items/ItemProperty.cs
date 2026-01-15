using System;
using System.IO;
using System.Linq;
using Core.ContentSerializer;

namespace Core.Items
{
    [Serializable]
    public struct ItemProperty : ICloneable
    {
        public const int Resizable_MassByLiter = 0;
        public const int Resizable_StackSize = 1;
        public const int Mass_MassByOne = 0;
        public const int Mass_VolumeByOne = 1;
        public const int Mass_StackSize = 2;
        public const int Liquid_MassByLiter = 0;
        public const int Container_Volume = 0;
        public const int Container_IncludeRules = 1;
        public const int Container_ExcludeRules = 2;
        public const int Equipable_SlotType = 0;
        public const int IdentifiableInstance_Identifier = 0;
        public string name;
        public ItemPropertyValue[] values;

        public object Clone()
        {
            return new ItemProperty {name = name, values = values.ToArray()};
        }
        
        public class Serializer : ISerializer<ItemProperty>
        {
            public void Serialize(ItemProperty obj, Stream stream)
            {
                stream.WriteInt(obj.values.Length);
                foreach (var itemPropertyValue in obj.values)
                {
                    stream.WriteString(itemPropertyValue.stringValue);
                    stream.WriteInt(itemPropertyValue.intValue);
                    stream.WriteFloat(itemPropertyValue.floatValue);
                }
            }

            public ItemProperty Deserialize(Stream stream)
            {
                var entity = new ItemProperty();
                Populate(stream, ref entity);
                return entity;
            }

            public void Populate(Stream stream, ref ItemProperty obj)
            {
                obj.values = new ItemPropertyValue[stream.ReadInt()];
                for (int i = 0; i < obj.values.Length; i++)
                {
                    obj.values[i] = new ItemPropertyValue {stringValue = stream.ReadString(), intValue = stream.ReadInt(), floatValue = stream.ReadFloat()};
                }
            }
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