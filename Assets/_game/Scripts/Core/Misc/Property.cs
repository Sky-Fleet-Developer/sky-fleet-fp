using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Core.ContentSerializer;

namespace Core.Misc
{
    [Serializable]
    public struct Property : ICloneable
    {
        public const string PositionPropertyName = "position";
        public const string RotationPropertyName = "rotation";
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
        public PropertyValue[] values;

        public Property(string name, params PropertyValue[] values) => (this.name, this.values) = (name, values);
        
        public object Clone()
        {
            return new Property {name = name, values = values.ToArray()};
        }
        
        public class Serializer : ISerializer<Property>
        {
            public void Serialize(Property obj, Stream stream)
            {
                stream.WriteString(obj.name);
                stream.WriteInt(obj.values.Length);
                foreach (var itemPropertyValue in obj.values)
                {
                    stream.WriteString(itemPropertyValue.stringValue);
                    stream.WriteInt(itemPropertyValue.intValue);
                    stream.WriteFloat(itemPropertyValue.floatValue);
                }
            }

            public Property Deserialize(Stream stream)
            {
                var entity = new Property();
                Populate(stream, ref entity);
                return entity;
            }

            public void Populate(Stream stream, ref Property obj)
            {
                obj.name = stream.ReadString();
                obj.values = new PropertyValue[stream.ReadInt()];
                for (int i = 0; i < obj.values.Length; i++)
                {
                    obj.values[i] = new PropertyValue {stringValue = stream.ReadString(), intValue = stream.ReadInt(), floatValue = stream.ReadFloat()};
                }
            }
        }
        
        private static readonly char[] EntityTagParameterSeparators = new [] {':', '=', ';'};

        public static bool TryParse(string str, out Property property)
        {
            var parameters = str.Split(EntityTagParameterSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parameters.Length == 0)
            {
                property = default;
                return false;
            }
            
            string nameTrim = parameters[0].Trim();
            if (parameters.Length == 1)
            {
                property = new Property{name = nameTrim};
                return true;
            }
            
            property = new Property{name = nameTrim, values = new PropertyValue[parameters.Length - 1]};
            for (var p = 1; p < parameters.Length; p++)
            {
                string pTrim = parameters[p].Trim();
                property.values[p-1] = new PropertyValue(pTrim);
            }
            return true;
        }
    }
    
    [Serializable]
    public struct PropertyValue
    {
        public string stringValue;
        public int intValue;
        public float floatValue;

        public PropertyValue(string sourceString)
        {
            int.TryParse(sourceString, out intValue);
            float.TryParse(sourceString, out floatValue);
            stringValue = sourceString;
        }
        
        public PropertyValue(int intValue)
        {
            stringValue = intValue.ToString(CultureInfo.InvariantCulture);
            this.intValue = intValue;
            floatValue = default;
        }
        
        public PropertyValue(float floatValue)
        {
            stringValue = floatValue.ToString(CultureInfo.InvariantCulture);
            this.floatValue = floatValue;
            intValue = default;
        }
    }
}