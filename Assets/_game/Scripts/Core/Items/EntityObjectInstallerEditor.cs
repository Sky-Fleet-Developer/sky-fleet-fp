using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Core.Ai;
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
using UnityEngine.Serialization;

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

        [ShowInInspector]
        [ValueDropdown("GetAbleSignatures")]
        public string Signature
        {
            get
            {
                //EnsureObjects();
                int property = FindOrAddProperty(ref itemDescription, Property.SignatureIdPropertyName, 1);
                return itemDescription.properties[property].values[Property.SignatureId_Signature].stringValue;
            }
            set
            {
                EnsureObjects();
                int property = FindOrAddProperty(ref itemDescription, Property.SignatureIdPropertyName, 1);
                itemDescription.properties[property].values[Property.SignatureId_Signature].stringValue = value;
            }
        }
        
        private IEnumerable<string> GetAbleSignatures()
        {
            return EditorReferences.RelationsTableEditor.GetAllRegisteredSignatures();
        }

        [ShowInInspector]
        public string SignatureManual
        {
            get => Signature;
            set => Signature = value;
        }

        private IItemObject _itemObjectEditor;
        private static Dictionary<string, IItemObject> _registeredGUIDsEditor = new();
        //[ShowInInspector] private List<AmmoItemWarp> _weapon = new();

        /*[Serializable]
        private class AmmoItemWarp
        {
            [InlineProperty]
            public ItemDescription weapon;
            [HideInInspector]
            public ItemDescription ammoItem;
            [ShowInInspector, InlineProperty, ValueDropdown(nameof(GetAbleItems))]
            public string Ammo
            {
                get => ammoItem?.signId;
                set
                {
                    if (ammoItem != null)
                    {
                        ammoItem.signId = value;
                    }
                    else
                    {
                        ammoItem = new ItemDescription{signId = value, amount = 100, gridSlot = "main"};
                        weapon.nestedItems ??= new();
                        weapon.nestedItems.Add(ammoItem);
                    }
                }
            }

            private static IEnumerable<string> GetAbleItems()
            {
                return EditorReferences.ItemsTableEditor.GetShellSigns();
            }
        }*/
        
        
        private void EnsureObjects()
        {
            _itemObjectEditor = GetComponent<IItemObject>();
        }

        private IEnumerable<string> GetAbleItems()
        {
            EnsureObjects();
            foreach (var p in EnumerateAbleItems(_itemObjectEditor.AssetId)) yield return p.Id;
        }

        private static IEnumerable<ItemSign> EnumerateAbleItems(string prefabGuid)
        {
            foreach (var item in EditorReferences.ItemsTableEditor.GetItems())
            {
                if (string.Equals(EditorReferences.ItemsTableEditor.GetItemPrefabGuid(item.Id), prefabGuid))
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
        
        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(_itemObjectEditor, ref itemDescription, this);
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
                SetupNestedItems(_itemObjectEditor, ref itemDescription, this);
            }
        }

        private static void SetupItem(IItemObject itemObject, ref ItemDescription itemDescription) //TODO: add nested items to nested items
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }

            int property;
            var sign = EditorReferences.ItemsTableEditor.GetItem(itemDescription.signId);
            if (sign.HasTag(ItemSign.IdentifiableTag))
            {
                property = FindOrAddProperty(ref itemDescription, ItemSign.IdentifiableTag, 1);
                ref string guid = ref itemDescription.properties[property].values[0].stringValue;
                bool alreadyRegistered = _registeredGUIDsEditor.TryGetValue(guid, out var c);
                if (string.IsNullOrEmpty(guid) || alreadyRegistered && c != itemObject)
                {
                    guid = Guid.NewGuid().ToString();
                    alreadyRegistered = false;
                }

                if (!alreadyRegistered)
                {
                    _registeredGUIDsEditor.Add(guid, itemObject);
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

        private static void SetupNestedItems(IItemObject itemObject, ref ItemDescription itemDescription, EntityObjectInstaller instance)
        {
            if (string.IsNullOrEmpty(itemDescription.signId))
            {
                return;
            }
            //instance._weapon.Clear();
            var sign = EditorReferences.ItemsTableEditor.GetItem(itemDescription.signId);
            if (sign.HasTag(ItemSign.ContainerTag))
            {
                CollectContainerContent(ref itemDescription, itemObject, instance);
            }
        }

        private static void CollectContainerContent(ref ItemDescription root, IItemObject itemObject,
            EntityObjectInstaller instance)
        {
            //root.nestedItems?.Clear();

            CollectChildrenRecursive(ref root, itemObject.transform);

            void CollectChildrenRecursive(ref ItemDescription root, Transform transform)
            {
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent(out IItemObject childItemObject))
                    {
                        AddNestedItem(ref root, childItemObject, instance);
                    }
                    else
                    {
                        CollectChildrenRecursive(ref root, child);
                    }
                }
            }
        }

        private static void AddNestedItem(ref ItemDescription root, IItemObject childItemObject,
            EntityObjectInstaller instance)
        {
            root.nestedItems ??= new ();
            var item = EnumerateAbleItems(childItemObject.AssetId).FirstOrDefault();
            if (item == null)
            {
                Debug.LogError($"Has no item {childItemObject.AssetId}");
            }

            ItemDescription description = null;

            var expectId = item?.Id ?? "_";
            description = root.nestedItems.FirstOrDefault(x =>
                x.signId == expectId && Mathf.Approximately(x.amount, 1) &&
                x.gridSlot == childItemObject.transform.name);
            
            bool isNew = description == null;
            description ??= new ItemDescription
            {
                signId = expectId,
                amount = 1,
                properties = new List<Property>(),
                gridSlot = childItemObject.transform.name
            };
            
            //var sign = EditorReferences.ItemsTableEditor.GetItem(description.signId);
            //if (sign.HasTag(ItemSign.KineticWeaponTag))
            //{
            //
            //    var ammo = description.nestedItems.FirstOrDefault(x => x.signId == ItemSign.ShellTag);
            //    instance._weapon.Add(new AmmoItemWarp{weapon = description, ammoItem = ammo});
            //}
            SetupItem(childItemObject, ref description);
            if (isNew)
            {
                root.nestedItems.Add(description);
            }
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
                            var prefabItem = EditorReferences.TablePrefabsEditor.GetItem(childItemObject.AssetId);
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
            var sign = EditorReferences.ItemsTableEditor.GetItem(itemDescription.signId);
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