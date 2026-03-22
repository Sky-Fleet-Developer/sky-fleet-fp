using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Character.Stuff;
using Core.ContentSerializer;
using Core.Misc;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;

namespace Core.Items
{
    public class ItemInstance : IDisposable, IEquatable<ItemInstance>, IDraggableItem, IPropertiesContainer
    {
        private ItemSign _sign;
        private float _amount;
        private List<Property> _properties = new();
        public ItemSign Sign => _sign;
        public float Amount => _amount;
        public IReadOnlyList<Property> Properties => _properties;
        public string Identifier => TryGetProperty(ItemSign.IdentifiableTag, out var property) ? property.values[Property.IdentifiableInstance_Identifier].stringValue : null;
        public bool IsContainer => _sign.HasTag(ItemSign.ContainerTag);
        public string ContainerKey => _containerKey;
        public bool IsUnique => _sign.HasTag(ItemSign.IdentifiableTag);
        public bool IsEmpty => _sign == null || _amount == 0;
        int IDraggableItem.Order => IsContainer ? 1 : 0;

        private string _containerKey = null;
        private static int _id = 0;
        private int _instanceId = _id++;
        private Action<string, string> _containerRegistrationCallback;
        private Action<string> _unbindInventoryToContainerSettings;
        public ItemInstance(){}

        public ItemInstance(ItemSign sign, ItemDescription description,
            Action<string, string> containerRegistrationCallback,
            Action<string> unbindInventoryToContainerSettings)
        {
            _unbindInventoryToContainerSettings = unbindInventoryToContainerSettings;
            _containerRegistrationCallback = containerRegistrationCallback;
            _amount = description.amount;
            _sign = sign;
            _properties = description.properties.DeepClone();
            if (IsUnique && IsContainer)
            {
                TrySetupContainerId();
                if (ContainerKey != null)
                {
                    _containerRegistrationCallback(ContainerKey, sign.Id);
                }
            }
        }

        private void TrySetupContainerId()
        {
            if (_sign.TryGetProperty(ItemSign.ContainerTag, out var property))
            {
                var postfix = property.values[Property.Container_GridPreset].stringValue;
                if (string.IsNullOrEmpty(postfix))
                {
                    _containerKey = Identifier;
                }
                else
                {
                    _containerKey = $"{Identifier}{StuffSlotsTable.GridIdentifierKey}{postfix}";
                }
            }
            else
            {
                _containerKey = null;
            }
        }

        public ItemInstance(ItemSign sign, float amount, Action<string, string> containerRegistrationCallback,
            Action<string> unbindInventoryToContainerSettings)
        {
            _unbindInventoryToContainerSettings = unbindInventoryToContainerSettings;
            _containerRegistrationCallback = containerRegistrationCallback;
            _amount = amount;
            _sign = sign;
            TrySetupUniqId(sign);
        }

        private void TrySetupUniqId(ItemSign sign, string uniqId = null)
        {
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                uniqId ??= Guid.NewGuid().ToString();
                _properties.Add(new Property{name = ItemSign.IdentifiableTag, values = new []{new PropertyValue{stringValue = uniqId}}});
                if(IsContainer)
                {
                    TrySetupContainerId();
                    _containerRegistrationCallback(ContainerKey, sign.Id);
                }
            }
            else if (!string.IsNullOrEmpty(uniqId))
            {
                Debug.LogError($"Error when creating item instance: Trying setup uniqId to item without Identifiable tag. Wanted id: {uniqId}, item sign: {sign.Id}");
            }
        }

        public void SetOwnership(string owner)
        {
            if (!TryGetProperty(ItemSign.OwnershipTag, out var property))
            {
                property = new Property { name = ItemSign.OwnershipTag, values = new[] { new PropertyValue { stringValue = owner } } };
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
            if (string.IsNullOrEmpty(ContainerKey))
            {
                value = null;
                return false;
            }
            value = ContainerKey;
            return true;
        }

        public bool TryGetProperty(string propertyName, out Property property)
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

        public Property EnsureProperty(string propertyName)
        {
            if (!TryGetProperty(propertyName, out var property))
            {
                property = new Property { name = propertyName };
                _properties.Add(property);
            }
            return property;
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

        public class Serializer : ISerializer<ItemInstance>
        {
            public void Serialize(ItemInstance obj, Stream stream)
            {
            }

            public void Populate(Stream stream, ref ItemInstance obj)
            {
            }
        }
    }
}