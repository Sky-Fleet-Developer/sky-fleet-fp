using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core.Configurations.GoogleSheets;
using Core.Items;
using Core.Misc;
using Core.Trading;
using UnityEngine;

namespace Core.Configurations
{
    [CreateAssetMenu(menuName = "SF/Configs/Items")]
    public class ItemsTable : Table<ItemsTable.RawItemSign>
    {
        public class RawItemSign
        {
            public string Id;
            public string[] TradeTags;
            public string[] Properties;
            public int BasicCost;
            public string TablePrefabOverride;
        }
        [System.Serializable]
        private class PrefabLink
        {
            public string signId;
            public string prefabGuid;
        }
        
        
        [SerializeField] private List<ItemSign> items;
        [SerializeField] private List<PrefabLink> linksToPrefabs;
        [SerializeField] private List<ContainerInfo> containerInfos;
        private Dictionary<string, ItemSign> _itemById;
        private Dictionary<string, PrefabLink> _prefabLinkById;
        private Dictionary<string, ContainerInfo> _containerById;
        public override string TableName => "Items";
        public IEnumerable<ItemSign> GetItems() => items;

        public string GetItemPrefabGuid(string itemId)
        {
            _prefabLinkById ??= linksToPrefabs.ToDictionary(x => x.signId);
            return _prefabLinkById[itemId].prefabGuid;
        }
        public ItemSign GetItem(string id)
        {
            _itemById ??= items.ToDictionary(x => x.Id);
            return _itemById[id];
        }

        public ContainerInfo GetContainer(string id)
        {
            _containerById ??= containerInfos.ToDictionary(x => x.SignId);
            return _containerById[id];
        }
        
        protected override RawItemSign[] Data
        {
            set
            {
                items = new List<ItemSign>(value.Length);
                linksToPrefabs = new List<PrefabLink>();
                containerInfos = new List<ContainerInfo>();
                _containerById?.Clear();
                _containerById = null;
                _itemById?.Clear();
                _itemById = null;
                _prefabLinkById?.Clear();
                _prefabLinkById = null;
                List<Property> properties = new List<Property>();
                List<string> tags = new List<string>();
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                for (int i = 0; i < value.Length; i++)
                {
                    var rawItemSign = value[i];

                    properties.Clear();
                    tags.Clear();
                    tags.AddRange(rawItemSign.TradeTags);
                    foreach (string entityTag in rawItemSign.Properties)
                    {
                        if (Property.TryParse(entityTag, out Property property))
                        {
                            if (property.values != null && property.values.Length != 0)
                            {
                                properties.Add(property);
                            }
                            tags.Add(property.name);
                        }
                    }
                    ItemSign newItem = new ItemSign(rawItemSign.Id, tags.ToArray(), properties.ToArray(), rawItemSign.BasicCost);
                    linksToPrefabs.Add(new PrefabLink{ signId = newItem.Id, prefabGuid = string.IsNullOrEmpty(rawItemSign.TablePrefabOverride) ? newItem.Id : rawItemSign.TablePrefabOverride });
                    items.Add(newItem);

                    if (newItem.TryGetProperty(ItemSign.ContainerTag, out Property containerProperty))
                    {
                        containerInfos.Add(new ContainerInfo(newItem.Id, containerProperty.values[Property.Container_Volume].floatValue,
                        containerProperty.values[Property.Container_IncludeRules].stringValue,
                        containerProperty.values[Property.Container_ExcludeRules].stringValue,
                        containerProperty.values[Property.Container_GridPreset].stringValue));
                    }
                }
            }
        }
    }
}