using System;
using System.Collections.Generic;
using Core.Utilities;
using UnityEditor;
using UnityEngine;

namespace Core.Items
{
    public class ItemInstance : IDisposable
    {
        private ItemSign _sign;
        private float _amount;
        private List<ItemProperty> _properties = new();
        public ItemSign Sign => _sign;
        public float Amount => _amount;
        public ItemInstance(){}
        public ItemInstance(ItemSign sign, float amount)
        {
            _amount = amount;
            _sign = sign;
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                _properties.Add(new ItemProperty{name = ItemSign.IdentifiableTag, values = new []{new ItemPropertyValue{stringValue = GUID.Generate().ToString()}}});
            }
        }

        public void SetOwnership(string owner)
        {
            _properties.Add(new ItemProperty{name = ItemSign.OwnershipTag, values = new []{new ItemPropertyValue{stringValue = owner}}});
        }

        public string GetOwnership()
        {
            if (TryGetProperty(ItemSign.OwnershipTag, out var property))
            {
                return property.values[0].stringValue;
            }
            return null;
        }
        
        public bool TryGetProperty(string propertyName, out ItemProperty property)
        {
            for (var i = 0; i < _properties.Count; i++)
            {
                if (_properties[i].name == propertyName)
                {
                    property = _properties[i];
                    return true;
                }
            }
            property = default;
            return false;
        }
        
        public float GetVolume()
        {
            return Sign.GetSingleVolume() * _amount;
        }
        
        public float GetMass()
        {
            return Sign.GetSingleMass() * _amount;
        }
        
        public ItemInstance Detach(float amountToDetach)
        {
            if (Sign.HasTag(ItemSign.MassTag))
            {
                amountToDetach = Mathf.Floor(amountToDetach);
            }
                
            if (amountToDetach < 0 || amountToDetach >= Amount)
            {
                throw new ArgumentException($"Invalid amount to detach: {amountToDetach} / {Amount}");
            }

            _amount -= amountToDetach;
            var newInstance = new ItemInstance(_sign, amountToDetach);
            newInstance._properties = _properties.DeepClone();
            return newInstance;
        }

        public void Merge(ItemInstance other)
        {
            if (!_sign.Equals(other._sign))
            {
                throw new ArgumentException($"Cant merge items with different signs ({_sign.Id}, {other._sign.Id})");
            }
            float finalAmount = _amount + other._amount;
            if (Sign.HasTag(ItemSign.MassTag))
            {
                finalAmount = Mathf.Floor(finalAmount);
            }

            _amount = finalAmount;
            other._amount = 0;
            other.Dispose();
        }

        public void Dispose()
        {
            _sign = null;
        }

        public bool IsCanMerge(ItemInstance other)
        {
            if (other == null) return false;
            if(ReferenceEquals(this, other)) return true;
            if(!_sign.Equals(other._sign)) return false;
            if(GetOwnership() != other.GetOwnership()) return false;
            if (TryGetProperty(ItemSign.IdentifiableTag, out var i1) ||
                other.TryGetProperty(ItemSign.IdentifiableTag, out var i2)) return false;
            return true;
        }
    }
}