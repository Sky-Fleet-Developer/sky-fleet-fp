using System;
using System.Collections.Generic;
using Core.Trading;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;

namespace Core.Items
{
    public class ItemInstance : IDisposable, IEquatable<ItemInstance>, IDraggableItem
    {
        private ItemSign _sign;
        private float _amount;
        private List<ItemProperty> _properties = new();
        public ItemSign Sign => _sign;
        public float Amount => _amount;
        public string Identifier => TryGetProperty(ItemSign.IdentifiableTag, out var property) ? property.values[ItemProperty.IdentifiableInstance_Identifier].stringValue : null;
        public bool IsContainer => _sign.HasTag(ItemSign.ContainerTag);
        public bool IsUnique => TryGetProperty(ItemSign.IdentifiableTag, out _);
        public bool IsEmpty => _sign == null || _amount == 0;
        int IDraggableItem.Order => IsContainer ? 1 : 0;
        
        private static int _id = 0;
        private int _instanceId = _id++;
        private Action<string, string> _containerRegistrationCallback;
        private Action<string> _unbindInventoryToContainerSettings;
        public ItemInstance(){}
        public ItemInstance(ItemSign sign, float amount, Action<string, string> containerRegistrationCallback,
            Action<string> unbindInventoryToContainerSettings)
        {
            _unbindInventoryToContainerSettings = unbindInventoryToContainerSettings;
            _containerRegistrationCallback = containerRegistrationCallback;
            _amount = amount;
            _sign = sign;
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                var guid = Guid.NewGuid().ToString();
                _properties.Add(new ItemProperty{name = ItemSign.IdentifiableTag, values = new []{new ItemPropertyValue{stringValue = guid}}});
                if(IsContainer)
                {
                    _containerRegistrationCallback(guid, sign.Id);
                }
            }
        }

        public void SetOwnership(string owner)
        {
            if (!TryGetProperty(ItemSign.OwnershipTag, out var property))
            {
                property = new ItemProperty { name = ItemSign.OwnershipTag, values = new[] { new ItemPropertyValue { stringValue = owner } } };
                _properties.Add(property);
            }
            else
            {
                property.values[0].stringValue = owner;
            }
        }

        public string GetOwnership()
        {
            if (TryGetProperty(ItemSign.OwnershipTag, out var property))
            {
                return property.values[0].stringValue;
            }
            return null;
        }

        public bool TryGetContainerKey(out string value)
        {
            if (!IsContainer)
            {
                value = null;
                return false;
            }
            if (!TryGetProperty(ItemSign.IdentifiableTag, out var identifiable))
            {
                value = null;
                return false;
            }
            value = identifiable.values[ItemProperty.IdentifiableInstance_Identifier].stringValue;
            return true;
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
        
        public ItemInstance Split(float amountToDetach)
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
            var newInstance = new ItemInstance(_sign, amountToDetach, _containerRegistrationCallback, _unbindInventoryToContainerSettings);
            foreach (var property in _properties)
            {
                if(property.name == ItemSign.IdentifiableTag) continue;
                newInstance._properties.Add(property);
            }
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
            if(IsContainer) _unbindInventoryToContainerSettings(Identifier);
            _sign = null;
        }

        public bool Equals(ItemInstance other)
        {
            if (other == null) return false;
            if(ReferenceEquals(this, other)) return true;
            if(!_sign.Equals(other._sign)) return false;
            if(GetOwnership() != other.GetOwnership()) return false;
            if (IsUnique || other.IsUnique) return false;
            return true;
        }

        public bool IsEqualsSignOrIdentity(ItemInstance other) => _sign.Equals(other._sign) && Identifier == other.Identifier;
    }
}