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
using Core.Structure.Serialization;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core.Items
{
    public partial class EntityObjectInstaller
    {
#if UNITY_EDITOR
        [ValueDropdown("GetAbleItems")]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public string ItemSignId
        {
            get => itemDescription.signId;
            set => itemDescription.signId = value;
        }

        private IItemObject _itemObjectEditor;
        private static ItemsTable _itemsTableEditor;
        private static TablePrefabs _tablePrefabsEditor;
        private static StuffSlotsTable _stuffSlotsTableEditor;
        private static Dictionary<string, IItemObject> RegisteredGUIDsEditor = new();

        private IEnumerable<string> GetAbleItems()
        {
            EnsureObjects();
            foreach (var p in EnumerateAbleItems(_itemObjectEditor.AssetId)) yield return p.Id;
        }

        private static IEnumerable<ItemSign> EnumerateAbleItems(string prefabGuid)
        {
            foreach (var item in _itemsTableEditor.GetItems())
            {
                if (string.Equals(_itemsTableEditor.GetItemPrefabGuid(item.Id), prefabGuid))
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

                SetupItem(_itemObjectEditor, ref itemDescription);
            }
            else
            {
                enabled = false;
            }
        }

        private void EnsureObjects()
        {
            _itemsTableEditor ??= Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<ItemsTable>().First();
            _tablePrefabsEditor ??= Resources.FindObjectsOfTypeAll<TablePrefabs>()[0];
            _stuffSlotsTableEditor ??=
                Resources.FindObjectsOfTypeAll<GameData>()[0].GetChildAssets<StuffSlotsTable>().First();
            _itemObjectEditor = GetComponent<IItemObject>();
        }

        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(_itemObjectEditor, ref itemDescription);
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(_itemObjectEditor, ref itemDescription);
            }
        }

        private static void SetupItem(IItemObject itemObject, ref ItemDescription itemDescription)
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }

            int property;
            var sign = _itemsTableEditor.GetItem(itemDescription.signId);
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                property = FindOrAddProperty(ref itemDescription, ItemSign.IdentifiableTag, 1);
                ref string guid = ref itemDescription.properties[property].values[0].stringValue;
                bool alreadyRegistered = RegisteredGUIDsEditor.TryGetValue(guid, out var c);
                if (string.IsNullOrEmpty(guid) || alreadyRegistered && c != itemObject)
                {
                    guid = Guid.NewGuid().ToString();
                    alreadyRegistered = false;
                }

                if (!alreadyRegistered)
                {
                    RegisteredGUIDsEditor.Add(guid, itemObject);
                }
            }

            property = FindOrAddProperty(ref itemDescription, Property.PositionPropertyName, 3);
            for (int i = 0; i < 3; i++)
            {
                itemDescription.properties[property].values[i] =
                    new PropertyValue(itemObject.transform.localPosition[i]);
            }

            property = FindOrAddProperty(ref itemDescription, Property.RotationPropertyName, 4);
            for (int i = 0; i < 4; i++)
            {
                itemDescription.properties[property].values[i] =
                    new PropertyValue(itemObject.transform.localRotation[i]);
            }

            if (itemObject is IBlock block)
            {
                foreach (var propertyInfo in block.GetBlockPlayerPropertiesCached())
                {
                    string value = propertyInfo.GetValue(block).ToString();
                    property = FindOrAddProperty(ref itemDescription, propertyInfo.Name, 1);
                    itemDescription.properties[property].values[0] = new PropertyValue(value);
                }
            }
        }

        private static void SetupNestedItems(IItemObject itemObject, ref ItemDescription itemDescription)
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }

            var sign = _itemsTableEditor.GetItem(itemDescription.signId);
            if (sign.HasTag(ItemSign.ContainerTag))
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
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent(out IItemObject childItemObject))
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

        private static void AddNestedItem(ref ItemDescription root, IItemObject childItemObject)
        {
            root.nestedItems ??= new List<ItemDescription>();
            var item = EnumerateAbleItems(childItemObject.AssetId).FirstOrDefault();
            if (item == null)
            {
                Debug.LogError($"Has no item {childItemObject.AssetId}");
            }

            var description = new ItemDescription
            {
                signId = item?.Id ?? "_",
                amount = 1,
                properties = new List<Property>(),
                gridSlot = childItemObject.transform.name
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

        [Button]
        private void PasteWiresConfigFromClipboard()
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard)) return;

            try
            {
                List<WireConfiguration> wires =
                    JsonConvert.DeserializeObject<List<WireConfiguration>>(GUIUtility.systemCopyBuffer);

                var prop = itemDescription.properties.FindIndex(x => x.name == Property.WiresPropertyName);
                if (prop == -1)
                {
                    itemDescription.properties.Add(default);
                    prop = itemDescription.properties.Count - 1;
                }

                itemDescription.properties[prop] = new Property(Property.WiresPropertyName,
                    wires.Select(x => new PropertyValue(x)).ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }
        }

        [Space(20)] [ShowInInspector]
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
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent(out IItemObject childItemObject))
                    {
                        var sign = EnumerateAbleItems(childItemObject.AssetId).FirstOrDefault();
                        if (sign == null)
                        {
                            var prefabItem = _tablePrefabsEditor.GetItem(childItemObject.AssetId);
                            if (prefabItem == null)
                            {
                                Debug.LogError(
                                    $"Prefab for object {childItemObject.transform.name} ({childItemObject.AssetId}) not found");
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

                                    if (nameChars[i] == '_') nameChars[i] = '-';
                                }

                                var s = _itemsFormat.Replace("{name}", nameChars.ToString())
                                    .Replace("{guid}", childItemObject.AssetId).Replace("{mass}",
                                        reference.Mass.ToString(CultureInfo.InvariantCulture));
                                sb.AppendLine(s);
                            }
                        }
                    }

                    CollectChildrenRecursive(child);
                }
            }
        }

        [Button]
        private void SendBlocksConfigToGridLocalSource()
        {
            EnsureObjects();
            if (!TryGetComponent(out IStructure structure))
            {
                return;
            }
            StructureGridLocalSource gridLocalSource = AssetDatabase
                .LoadAssetAtPath<GameData>("Assets/_game/Data/Resources/GameData.asset")
                .GetChildAssets<StructureGridLocalSource>().First();
            var sign = _itemsTableEditor.GetItem(itemDescription.signId);
            if (!sign.TryGetProperty(ItemSign.ContainerTag, out var property))
            {
                return;
            }

            var gridId = property.values[Property.Container_GridPreset].stringValue;
            gridLocalSource.AddOrReplaceBlocksConfig(gridId, new BlocksConfiguration(structure.transform.gameObject));
        }
#endif
    }
}