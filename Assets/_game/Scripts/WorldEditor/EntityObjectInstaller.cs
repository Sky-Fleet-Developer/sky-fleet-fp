using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Core.Character.Stuff;
using Core.Configurations;
using Core.Data;
using Core.Items;
using Core.Misc;
using Core.Structure;
using Core.Utilities;
using Runtime.Items;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
namespace WorldEditor
{
    [ExecuteInEditMode, RequireComponent(typeof(IItemObject))]
    public class EntityObjectInstaller : MonoBehaviour
    {
        private const string GuidKey = "ItemIdentifier";

        [ValueDropdown("GetAbleItems")]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public string ItemSignId
        {
            get => itemDescription.signId;
            set => itemDescription.signId = value;
        }

        public ItemDescription itemDescription = new ();
        public IItemObject itemObject;
        private static ItemsTable _itemsTable;
        private static TablePrefabs _tablePrefabs;
        private static StuffSlotsTable _stuffSlotsTable;
        private IEnumerable<string> GetAbleItems()
        {
            EnsureObjects();
            foreach (var p in EnumerateAbleItems(itemObject.Guid)) yield return p.Id;
        }

        private static IEnumerable<ItemSign> EnumerateAbleItems(string prefabGuid)
        {
            foreach (var item in _itemsTable.GetItems())
            {
                if (string.Equals(_itemsTable.GetItemPrefabGuid(item.Id), prefabGuid))
                {
                    yield return item;
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                if (itemDescription.amount <= 0)
                {
                    itemDescription.amount = 1;
                }
                SetupItem(itemObject, ref itemDescription);
            }
            else
            {
                enabled = false;
            }
        }

        private void EnsureObjects()
        {
            _itemsTable ??= Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<ItemsTable>().First();
            _tablePrefabs ??= Resources.FindObjectsOfTypeAll<TablePrefabs>()[0];
            _stuffSlotsTable ??= Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<StuffSlotsTable>().First();
            itemObject = GetComponent<IItemObject>();
        }

        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(itemObject, ref itemDescription);
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(itemObject, ref itemDescription);
            }
        }

        private static void SetupItem(IItemObject itemObject, ref ItemDescription itemDescription)
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }
            int property;
            var sign = _itemsTable.GetItem(itemDescription.signId);
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                var customData = itemObject.transform.GetOrAddCustomObjectData();
                if (!customData.TryGetData(GuidKey, out var guid))
                {
                    guid = Guid.NewGuid().ToString();
                    customData.SetData(GuidKey, guid);
                }
                
                property = FindOrAddProperty(ref itemDescription, ItemSign.IdentifiableTag, 1);
                itemDescription.properties[property].values[0] = new PropertyValue(guid);
            }
                
            property = FindOrAddProperty(ref itemDescription, Property.PositionPropertyName, 3);
            for (int i = 0; i < 3; i++)
            {
                itemDescription.properties[property].values[i] = new PropertyValue(itemObject.transform.localPosition[i]);
            }

            property = FindOrAddProperty(ref itemDescription, Property.RotationPropertyName, 4);
            for (int i = 0; i < 4; i++)
            {
                itemDescription.properties[property].values[i] = new PropertyValue(itemObject.transform.localRotation[i]);
            }
        }
        
        private static void SetupNestedItems(IItemObject itemObject, ref ItemDescription itemDescription)
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }
            var sign = _itemsTable.GetItem(itemDescription.signId);
            if(sign.HasTag(ItemSign.ContainerTag))
            {
                CollectContainerContent(ref itemDescription, itemObject);
            }
        }

        private static void CollectContainerContent(ref ItemDescription root, IItemObject itemObject)
        {
            root.nestedItems?.Clear();
    
            CollectChildrenRecursive(ref root, itemObject.transform);
            void CollectChildrenRecursive(ref ItemDescription root, Transform transform)
            {
                foreach(Transform child in transform)
                {
                    if (child.TryGetComponent(out ItemObject childItemObject))
                    {
                        AddNestedItem(ref root, childItemObject);
                    }
                    else
                    {
                        CollectChildrenRecursive(ref root, child);
                    }
                }
            }
        }

        private static void AddNestedItem(ref ItemDescription root, ItemObject childItemObject)
        {
            root.nestedItems ??= new List<ItemDescription>();
            var description = new ItemDescription
            {
                signId = EnumerateAbleItems(childItemObject.Guid).FirstOrDefault()?.Id ?? "_",
                amount = 1,
                properties = new List<Property>(),
                gridSlot = childItemObject.name
            };
            SetupItem(childItemObject, ref description);
            root.nestedItems.Add(description);
        }

        private static int FindOrAddProperty(ref ItemDescription itemDescription, string propertyName, int valueCount)
        {
            var propertyIndex = itemDescription.properties.FindIndex(x => x.name == propertyName);
            if (propertyIndex != -1)
            {
                return propertyIndex;
            }

            itemDescription.properties.Add(new Property(propertyName, new PropertyValue[valueCount]));
            return itemDescription.properties.Count - 1;
        }

        [ShowInInspector]
        private static string _itemsFormat = "{name}_part\tblock\tmass: {mass}; 1; 1\t1\t{guid}";
        [Button]
        private void CollectNonExistItemsToClipboard()
        {
            EnsureObjects();
            StringBuilder sb = new();
            HashSet<IBlock> blocks = new();
            CollectChildrenRecursive(transform);
            GUIUtility.systemCopyBuffer = sb.ToString();
            void CollectChildrenRecursive(Transform transform)
            {
                foreach(Transform child in transform)
                {
                    if (child.TryGetComponent(out ItemObject childItemObject))
                    {
                        var sign = EnumerateAbleItems(childItemObject.Guid).FirstOrDefault();
                        if (sign == null)
                        {
                            var prefabItem = _tablePrefabs.GetItem(childItemObject.Guid);
                            if (prefabItem == null)
                            {
                                Debug.LogError($"Prefab for object {childItemObject.name} ({childItemObject.Guid}) not found");
                            }
                            else
                            {
                                var reference = prefabItem.GetReferenceInEditor().GetComponent<IBlock>();
                                if (!blocks.Add(reference))
                                {
                                    continue;
                                }
                                StringBuilder nameChars = new StringBuilder(reference.transform.name);
                                for (var i = 0; i < nameChars.Length; i++)
                                {
                                    if (char.IsUpper(nameChars[i]))
                                    {
                                        if (i != 0 && nameChars[i - 1] != '-')
                                        {
                                            nameChars.Insert(i++, '-');
                                        }
                                        nameChars[i] = char.ToLower(nameChars[i]);
                                        continue;
                                    }
                                    if(nameChars[i] == '_') nameChars[i] = '-';
                                }
                                var s = _itemsFormat.Replace("{name}", nameChars.ToString())
                                    .Replace("{guid}", childItemObject.Guid).Replace("{mass}", reference.Mass.ToString(CultureInfo.InvariantCulture));
                                sb.AppendLine(s);
                            }
                        }
                    }

                    CollectChildrenRecursive(child);
                }
            }
        }
    }
}
#endif
