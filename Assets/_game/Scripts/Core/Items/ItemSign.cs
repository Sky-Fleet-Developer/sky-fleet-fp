using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Items
{
    [Serializable]
    public class ItemSign : IEquatable<ItemSign>
    {
        public const string LiquidTag = "liquid";
        public const string MassTag = "mass";
        public const string ResizableTag = "resizable";
        public const string ContainerTag = "container";
        public const string IdentifiableTag = "identifiable"; //usings for assign uniq id to item to recognize its inventory or etc
        public const string OwnershipTag = "ownership"; //usings for assign owner to item
        public const string EquipableTag = "equipable";
        public const string AllTag = "All";
        
        [SerializeField] private string id;
        [SerializeField] private string[] tags;
        [SerializeField] private int basicCost;
        [SerializeField] private ItemProperty[] properties;
        
        public string Id => id;
        public IEnumerable<string> Tags => tags;
        public int BasicCost => basicCost;

        public ItemSign(){}
        public ItemSign(string id, string[] tags, ItemProperty[] properties, int basicCost)
        {
            this.id = id;
            this.tags = tags;
            this.properties = properties;
            this.basicCost = basicCost;
        }

        public bool HasTag(string tag)
        {
            if (tag == AllTag)
            {
                return true;
            }
            for (var i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetProperty(string propertyName, out ItemProperty property)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].name == propertyName)
                {
                    property = properties[i];
                    return true;
                }
            }
            property = default;
            return false;
        }

        public float GetStackSize()
        {
            if (TryGetProperty(MassTag, out ItemProperty massProperty))
            {
                return massProperty.values[ItemProperty.Mass_StackSize].floatValue;
            }
            else if (TryGetProperty(ResizableTag, out ItemProperty resizableProperty))
            {
                return resizableProperty.values[ItemProperty.Resizable_StackSize].floatValue;
            }
            Debug.LogError($"Has no volume properties on ItemSign {Id}");
            return 1;
        }

        public float GetSingleVolume()
        {
            if (TryGetProperty(MassTag, out ItemProperty massProperty))
            {
                return massProperty.values[ItemProperty.Mass_VolumeByOne].floatValue;
            }
            return 1;
        }
        
        public float GetSingleMass()
        {
            if (TryGetProperty(MassTag, out ItemProperty massProperty))
            {
                return massProperty.values[ItemProperty.Mass_MassByOne].floatValue;
            }
            else if(TryGetProperty(ResizableTag, out ItemProperty resizableProperty))
            {
                return resizableProperty.values[ItemProperty.Resizable_MassByLiter].floatValue;
            }
            Debug.LogError($"Has no mass properties on ItemSign {Id}");
            return 1;
        }
        
        public bool Equals(ItemSign other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemSign)obj);
        }

        public override int GetHashCode()
        {
            return (id != null ? id.GetHashCode() : 0);
        }
    }
}