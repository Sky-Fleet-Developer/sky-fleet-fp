using System;
using System.Collections.Generic;
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
        
        [SerializeField] private ItemSign[] items;
        [SerializeField] private CrateInfo[] crates;
        public override string TableName => "Items";
        public ItemSign[] Items => items;

        private static readonly char[] EntityTagParameterSeparators = new [] {':', '='};
        private const string IsCrateTag = "crate";
        private const string MassParameter = "mass";
        private const float MinimalMass = 0.01f;
        protected override RawItemSign[] Data
        {
            set
            {
                List<string> tags = new List<string>();
                List<CrateInfo> crates = new List<CrateInfo>();
                items = new ItemSign[value.Length];
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
                    items[i] = newItem;
                }

                this.crates = crates.ToArray();
            }
        }
    }
}