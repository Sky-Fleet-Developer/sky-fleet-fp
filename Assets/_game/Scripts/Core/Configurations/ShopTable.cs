using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core.Configurations.GoogleSheets;
using Core.Items;
using Core.Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Configurations
{
    [Serializable]
    public class ShopSettings
    {
        private readonly char[] _combinationSeparators = new[] { '&', ' ' };
        private readonly char[] _ruleSeparators = new[] { ':' };
        // Table values
        [SerializeField] private string id;
        private string[] includeItemsTags;
        private string[] excludeItemsTags;
        private string[] costRules;
        private string[] buyoutCostRules;
        // Postprocessed values
        [SerializeField] private CostRule[] costRulesProcessed;
        [SerializeField] private CostRule[] buyoutCostRulesProcessed;
        [SerializeField] private TagCombination[] includeTags;
        [SerializeField] private TagCombination[] excludeTags;

        public void Postprocess()
        {
            includeTags = new TagCombination[includeItemsTags.Length];
            for (var i = 0; i < includeTags.Length; i++)
            {
                includeTags[i].tags = includeItemsTags[i].Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
            }

            excludeTags = excludeItemsTags == null ? Array.Empty<TagCombination>() :  new TagCombination[excludeItemsTags.Length];
            for (var i = 0; i < excludeTags.Length; i++)
            {
                excludeTags[i].tags = excludeItemsTags[i].Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
            }
            UnpackCostRules(out costRulesProcessed, costRules);
            UnpackCostRules(out buyoutCostRulesProcessed, buyoutCostRules);
        }

        private void UnpackCostRules(out CostRule[] output, string[] input)
        {
            if (input == null)
            {
                output = Array.Empty<CostRule>();
                return;
            }

            int nonEmptyCounter = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (!string.IsNullOrEmpty(input[i]))
                {
                    nonEmptyCounter++;
                }
            }

            if (nonEmptyCounter == 0)
            {
                output = Array.Empty<CostRule>();
                return;
            }
            
            output = new CostRule[input.Length];
            for (var i = 0; i < output.Length; i++)
            {
                if(string.IsNullOrEmpty(input[i])) continue;
                var ruleProperties = input[i].Split(_ruleSeparators, StringSplitOptions.RemoveEmptyEntries);
                output[i].tags.tags = ruleProperties[0].Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
                output[i].value = float.Parse(ruleProperties[1], CultureInfo.InvariantCulture);
            }
        }

        public string Id => id;

        public bool IsItemMatch(ItemSign item)
        {
            bool match = false;
            for (int i = 0; i < includeTags.Length; i++)
            {
                if (includeTags[i].IsItemMatch(item))
                {
                    match = true;
                    break;
                }
            }

            if (!match)
            {
                return false;
            }
            
            for (var i = 0; i < excludeTags.Length; i++)
            {
                if (!excludeTags[i].IsEmpty && excludeTags[i].IsItemMatch(item))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetSellCost(ItemSign item)
        {
            float mul = GetSellFactor(item);
            return Mathf.RoundToInt(item.BasicCost * mul);
        }

        private float GetSellFactor(ItemSign item)
        {
            float mul = 1;
            for (var i = 0; i < costRulesProcessed.Length; i++)
            {
                if (costRulesProcessed[i].tags.IsItemMatch(item))
                {
                    mul *= costRulesProcessed[i].value;
                }
            }

            return mul;
        }

        public int GetBuyoutCost(ItemInstance item)
        {
            float mul = GetSellFactor(item.Sign);
            for (var i = 0; i < buyoutCostRulesProcessed.Length; i++)
            {
                if (buyoutCostRulesProcessed[i].tags.IsItemMatch(item.Sign))
                {
                    mul *= buyoutCostRulesProcessed[i].value;
                }
            }
            return Mathf.RoundToInt(item.Sign.BasicCost * mul);
        }

        public IEnumerable<CostRule> GetSellCostRules() => costRulesProcessed;
    }
    [CreateAssetMenu(menuName = "SF/Configs/Shops")]
    public class ShopTable : Table<ShopSettings>
    {
        public override string TableName => "Shop";
        [SerializeField] private ShopSettings[] data;
        private Dictionary<string, ShopSettings> _dictionary;
        protected override ShopSettings[] Data
        {
            set
            {
                data = value;
                foreach (var item in data)
                {
                    item.Postprocess();
                }
            }
        }

        public bool TryGetSettings(string id, out ShopSettings settings)
        {
            _dictionary ??= data.ToDictionary(v => v.Id);
            return _dictionary.TryGetValue(id, out settings);
        }
    }
}