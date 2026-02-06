using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Core.ContentSerializer;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Misc
{
    [Serializable]
    public struct Property : ICloneable
    {
        public const string PositionPropertyName = "position";
        public const string RotationPropertyName = "rotation";
        public const string SiblingPropertyName = "sibling";
        public const string PathPropertyName = "path";
        public const string ConstantFieldsPropertyName = "constant_fiels";
        public const string WiresPropertyName = "wires";
        public const string AutoConnectPowerWirePropertyName = "a_con_pow_count";
        public const int Resizable_MassByLiter = 0;
        public const int Resizable_StackSize = 1;
        public const int Mass_MassByOne = 0;
        public const int Mass_VolumeByOne = 1;
        public const int Mass_StackSize = 2;
        public const int Liquid_MassByLiter = 0;
        public const int Container_Volume = 0;
        public const int Container_IncludeRules = 1;
        public const int Container_ExcludeRules = 2;
        public const int Container_GridPreset = 3;
        public const int Equipable_SlotType = 0;
        public const int IdentifiableInstance_Identifier = 0;
        public string name;
        public PropertyValue[] values;

        public Property(string name, params PropertyValue[] values) => (this.name, this.values) = (name, values);
        
        public object Clone()
        {
            return new Property {name = name, values = values.ToArray()};
        }
        
        public class Serializer : ISerializer<Property> //TODO: optimize serialization, remove unused fields
        {
            public void Serialize(Property obj, Stream stream)
            {
                stream.WriteString(obj.name);
                stream.WriteInt(obj.values.Length);
                foreach (var itemPropertyValue in obj.values)
                {
                    stream.WriteString(itemPropertyValue.stringValue ?? "");
                    stream.WriteInt(itemPropertyValue.intValue);
                    stream.WriteFloat(itemPropertyValue.floatValue);
                    if (itemPropertyValue.objectValue is { Length: > 0 })
                    {
                        stream.WriteInt(itemPropertyValue.objectValue.Length);
                        stream.Write(itemPropertyValue.objectValue);
                    }
                    else
                    {
                        stream.WriteInt(0);
                    }
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
                    int objectValueLength = stream.ReadInt();
                    
                    if (objectValueLength <= 0) continue;
                    
                    obj.values[i].objectValue = new byte[objectValueLength];
                    int readBytesAmount = stream.Read(obj.values[i].objectValue, 0, objectValueLength);
                    if (readBytesAmount != objectValueLength)
                    {
                        throw new Exception(
                            $"Failed to read object value. Expected {objectValueLength} bytes, but read {readBytesAmount} bytes.");
                    }
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
                if (pTrim.Length == 1 && pTrim[0] == '_')
                {
                    pTrim = "";
                }
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
        public byte[] objectValue;
        private bool _isObjectValueDirty;
        private object _objectValueCached;

        public T GetObjectValue<T>()
        {
            if (_objectValueCached != null && !_isObjectValueDirty)
            {
                return (T)_objectValueCached;
            }

            if (objectValue == null || objectValue.Length == 0)
            {
                Debug.LogError($"There is no object value in property. stringValue: {stringValue}");
                return default;
            }
            using var stream = new MemoryStream(objectValue);
            _objectValueCached = Serializers.GetSerializer(typeof(T)).Deserialize(stream);
            _isObjectValueDirty = false;
            return (T)_objectValueCached;
        }

        public void SetObjectValue<T>(T value)
        {
            SetObjectValuePrivate(value);
        }
        
        private void SetObjectValuePrivate(object value)
        {
            _isObjectValueDirty = true;
            using var stream = new MemoryStream();
            Serializers.GetSerializer(value.GetType()).Serialize(value, stream);
            objectValue = stream.ToArray();
        }

        public PropertyValue([NotNull] object sourceObject)
        {
            intValue = default;
            floatValue = default;
            stringValue = null;
            _isObjectValueDirty = true;
            _objectValueCached = sourceObject;
            objectValue = null;
            SetObjectValuePrivate(sourceObject);
        }

        public PropertyValue(string sourceString)
        {
            int.TryParse(sourceString, out intValue);
            float.TryParse(sourceString, out floatValue);
            stringValue = sourceString;
            objectValue = null;
            _isObjectValueDirty = true;
            _objectValueCached = null;
        }
        
        public PropertyValue(int intValue)
        {
            stringValue = intValue.ToString(CultureInfo.InvariantCulture);
            this.intValue = intValue;
            floatValue = default;
            objectValue = null;
            _isObjectValueDirty = true;
            _objectValueCached = null;
        }
        
        public PropertyValue(float floatValue)
        {
            stringValue = floatValue.ToString(CultureInfo.InvariantCulture);
            this.floatValue = floatValue;
            intValue = default;
            objectValue = null;
            _isObjectValueDirty = true;
            _objectValueCached = null;
        }
    }
}