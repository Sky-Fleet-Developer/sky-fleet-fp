using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core.Configurations.GoogleSheets;
using Core.Items;
using Core.Trading;
using UnityEngine;

namespace Core.Configurations
{
    [CreateAssetMenu(menuName = "Configs/Items")]
    public class ItemsTable : Table<ItemsTable.RawItemSign>
    {
        public class RawItemSign
        {
            public string Id;
            public string[] TradeTags;
            public string[] Properties;
            public int BasicCost;
            public string TablePrefab;
        }
        [System.Serializable]
        private class PrefabLink
        {
            public string signId;
            public string prefabGuid;
        }
        
        private static readonly char[] EntityTagParameterSeparators = new [] {':', '=', ' ', ';'};
        
        [SerializeField] private List<ItemSign> items;
        [SerializeField] private List<PrefabLink> linksToPrefabs;
        private Dictionary<string, ItemSign> _itemById;
        private Dictionary<string, PrefabLink> _prefabLinkById;
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
        
        protected override RawItemSign[] Data
        {
            set
            {
                items = new List<ItemSign>(value.Length);
                linksToPrefabs = new List<PrefabLink>();
                List<ItemProperty> properties = new List<ItemProperty>();
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
                        var parameters = entityTag.Split(EntityTagParameterSeparators, StringSplitOptions.RemoveEmptyEntries);
                        tags.Add(parameters[0]);
                        if (parameters.Length == 1)
                        {
                            continue;
                        }
                        var property = new ItemProperty{name = parameters[0], values = new ItemPropertyValue[parameters.Length - 1]};
                        
                        for (var p = 1; p < parameters.Length; p++)
                        {
                            if (int.TryParse(parameters[p], out int intValue))
                            {
                                property.values[p-1].intValue = intValue;
                            }
                            if (float.TryParse(parameters[p], out float floatValue))
                            {
                                property.values[p-1].floatValue = floatValue;
                            }
                            property.values[p-1].stringValue = parameters[p];
                        }

                        properties.Add(property);
                    }
                    ItemSign newItem = new ItemSign(rawItemSign.Id, tags.ToArray(), properties.ToArray(), rawItemSign.BasicCost);
                    linksToPrefabs.Add(new PrefabLink{ signId = newItem.Id, prefabGuid = rawItemSign.TablePrefab });
                    items.Add(newItem);
                }
            }
        }
    }
}