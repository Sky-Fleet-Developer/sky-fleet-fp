using System;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations.GoogleSheets;
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
            public string[] EntityTags;
            public int BasicCost;
            public string TablePrefab;
        }
        [System.Serializable]
        private class PrefabLink
        {
            public string signId;
            public string prefabGuid;
        }
        
        private static readonly char[] EntityTagParameterSeparators = new [] {':', '='};
        private const string IsCrateTag = "crate";
        private const string MassParameter = "mass";
        private const float MinimalMass = 0.01f;
        
        [SerializeField] private List<ItemSign> items;
        [SerializeField] private List<CrateInfo> crates;
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
                List<string> tags = new List<string>();
                crates = new List<CrateInfo>();
                items = new List<ItemSign>(value.Length);
                linksToPrefabs = new List<PrefabLink>();
                for (int i = 0; i < value.Length; i++)
                {
                    var rawItemSign = value[i];
                    tags.Clear();

                    float mass = 0;
                    tags.AddRange(rawItemSign.TradeTags);
                    foreach (string entityTag in rawItemSign.EntityTags)
                    {
                        var parameters = entityTag.Split(EntityTagParameterSeparators, StringSplitOptions.RemoveEmptyEntries);
                        if (parameters.Length == 1)
                        {
                            tags.Add(parameters[0]);
                        }
                        else if (parameters[0] == IsCrateTag)
                        {
                            int capacity = parameters.Length == 3 && int.TryParse(parameters[2], out int val) ? val : 1;
                            crates.Add(new CrateInfo(rawItemSign.Id, parameters[1], capacity));
                            tags.Add(IsCrateTag);
                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (items[j].Id == parameters[1])
                                {
                                    mass = items[j].Mass * capacity;
                                    break;
                                }
                            }
                        }
                        else if (parameters[0] == MassParameter)
                        {
                            float.TryParse(parameters[1], out mass);
                        }
                    }
                    ItemSign newItem = new ItemSign(rawItemSign.Id, tags.ToArray(), rawItemSign.BasicCost, mass < MinimalMass ? MinimalMass : mass);
                    linksToPrefabs.Add(new PrefabLink{ signId = newItem.Id, prefabGuid = rawItemSign.TablePrefab });
                    items.Add(newItem);
                }
            }
        }
    }
}