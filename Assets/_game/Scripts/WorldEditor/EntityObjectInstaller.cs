using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations;
using Core.Data;
using Core.Items;
using Core.Misc;
using Core.World;
using Runtime.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldEditor
{
    [ExecuteInEditMode, RequireComponent(typeof(ItemObject))]
    public class EntityObjectInstaller : MonoBehaviour
    {
        [ValueDropdown("GetAbleItems")]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public string ItemSignId
        {
            get => itemDescription.signId;
            set => itemDescription.signId = value;
        }

        public ItemDescription itemDescription;
        public ItemObject itemObject;
        public string guid;
        delegate void PropertyRunner(ref Property property);
        #if UNITY_EDITOR
        private static ItemsTable _itemsTable;
        private IEnumerable<string> GetAbleItems()
        {
            _itemsTable ??= Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<ItemsTable>().First();
            foreach (var item in _itemsTable.GetItems())
            {
                if (string.Equals(_itemsTable.GetItemPrefabGuid(item.Id), itemObject.Guid))
                {
                    yield return item.Id;
                }
            }
        }
        #endif
        private void Update()
        {
            if (!Application.isPlaying)
            {
                _itemsTable ??= Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<ItemsTable>().First();
                itemObject = GetComponent<ItemObject>();
                var sign = _itemsTable.GetItem(itemDescription.signId);
                if (sign.HasTag(ItemSign.IdentifiableTag))
                {
                    guid ??= Guid.NewGuid().ToString();
                    ExecuteProperty(ItemSign.IdentifiableTag, 1, (ref Property property) => { property.values[0] = new PropertyValue(guid); });
                }

                ExecuteProperty(Property.PositionPropertyName, 3, (ref Property property) =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        property.values[i] = new PropertyValue(transform.localPosition[i]);
                    }
                });
                ExecuteProperty(Property.RotationPropertyName, 4, (ref Property property) =>
                {
                    for (int i = 0; i < 4; i++)
                    {
                        property.values[i] = new PropertyValue(transform.localRotation[i]);
                    }
                });
            }
        }
        
        private void ExecuteProperty(string propertyName, int valuesCount, PropertyRunner runner)
        {
            var propertyIndex = itemDescription.properties.FindIndex(x => x.name == propertyName);
            if (propertyIndex == -1)
            {
                itemDescription.properties.Add(new Property(propertyName, new PropertyValue[valuesCount]));
                propertyIndex = itemDescription.properties.Count - 1;
            }
            var property = itemDescription.properties[propertyIndex];
            runner(ref property);
            itemDescription.properties[propertyIndex] = property;
        }
    }
}