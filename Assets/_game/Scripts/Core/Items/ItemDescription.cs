using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Character.Stuff;
using Core.ContentSerializer;
using Core.Misc;
using Core.Trading;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace Core.Items
{
    [Serializable]
    public class ItemDescription : IPropertiesContainer
    {
#if UNITY_EDITOR
        [ShowInInspector, ValueDropdown(nameof(SignIds)), PropertyOrder(-10)]
        private string SelectedSignId
        {
            get => signId;
            set => signId = value;
        }
        private static IEnumerable<string> SignIds => EditorReferences.ItemsTableEditor.GetItems().Select(x => x.Id);
#endif
        public string signId;
        public float amount;
        public string gridSlot;
        public List<Property> properties;
        [CanBeNull, ShowInInspector] public List<ItemDescription> nestedItems = new();
        public IReadOnlyList<Property> Properties => properties;
        public ItemDescription(){}

        public ItemDescription(ItemInstance instance)
        {
            signId = instance.Sign.Id;
            amount = instance.Amount;
            properties = instance.Properties.ToList();
            nestedItems = null;
            gridSlot = null;
        }
        
        public bool TryGetProperty(string propertyName, out Property property)
        {
            for (var i = 0; i < properties.Count; i++)
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

        public void CollectNestedItems(BankSystem bankSystem)
        {
            if (!TryGetProperty(ItemSign.IdentifiableTag, out var identifiable))
            {
                return;
            }
            if (!TryGetProperty(ItemSign.ContainerTag, out var container))
            {
                return;
            }

            var postfix = container.values[Property.Container_GridPreset].stringValue;
            string id = identifiable.values[Property.IdentifiableInstance_Identifier].stringValue;
            if (!string.IsNullOrEmpty(postfix))
            {
                id = $"{id}{StuffSlotsTable.GridIdentifierKey}{postfix}";
            }
            var inv = bankSystem.GetOrCreateInventory(id);
            if (inv.IsEmpty)
            {
                return;
            }

            nestedItems = new();

            foreach (var itemInstance in inv.GetItems())
            {
                nestedItems.Add(new ItemDescription(itemInstance));
                nestedItems[^1].CollectNestedItems(bankSystem);
            }
        }

        public class Serializer : ISerializer<ItemDescription>
        {
            private static readonly ISerializer PropertySerializer = Serializers.GetSerializer(typeof(Property));

            public void Serialize(ItemDescription obj, Stream stream)
            {
                stream.WriteString(obj.signId);
                stream.WriteString(obj.gridSlot ?? "");
                stream.WriteFloat(obj.amount);
                stream.WriteInt(obj.properties?.Count ?? 0);
                if (obj.properties != null)
                {
                    foreach (Property property in obj.properties)
                    {
                        PropertySerializer.Serialize(property, stream);
                    }
                }

                stream.WriteInt(obj.nestedItems?.Count ?? 0);
                if (obj.nestedItems != null)
                {
                    foreach (ItemDescription nestedItem in obj.nestedItems)
                    {
                        Serialize(nestedItem, stream);
                    }
                }
            }

            public void Populate(Stream stream, ref ItemDescription obj)
            {
                obj.signId = stream.ReadString();
                obj.gridSlot = stream.ReadString();
                obj.amount = stream.ReadFloat();
                var propertyCount = stream.ReadInt();
                obj.properties = new List<Property>(propertyCount);
                for (var i = 0; i < propertyCount; i++)
                {
                    obj.properties.Add((Property)PropertySerializer.Deserialize(stream));
                }

                int nestedItemCount = stream.ReadInt();
                if (nestedItemCount > 0)
                {
                    obj.nestedItems = new List<ItemDescription>(nestedItemCount);

                    for (var i = 0; i < nestedItemCount; i++)
                    {
                        obj.nestedItems.Add((this as ISerializer<ItemDescription>).Deserialize(stream));
                    }
                }
            }
        }


    }
}