using System;
using System.Collections.Generic;
using System.Globalization;
using Core.Configurations.GoogleSheets;
using Core.Trading;
using UnityEngine;

namespace Core.Configurations
{
    [Serializable]
    public class ShopSettings
    {
        [Serializable]
        private struct TagCombination
        {
            public string[] tags;

            public bool IsItemMatch(ItemSign item)
            {
                for (var i = 0; i < tags.Length; i++)
                {
                    if (!item.HasTag(tags[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [Serializable]
        private struct CostRule
        {
            public float value;
            public TagCombination tags;
        }

        private readonly char[] _combinationSeparators = new[] { '&', ' ' };
        private readonly char[] _ruleSeparators = new[] { ':' };
        // Table values
        [SerializeField] private string id;
        private string[] includeItemsTags;
        private string[] excludeItemsTags;
        private string[] costRules;
        // Postprocessed values
        [SerializeField] private CostRule[] rules;
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

            rules = costRules == null ? Array.Empty<CostRule>() : new CostRule[costRules.Length];
            for (var i = 0; i < rules.Length; i++)
            {
                var ruleProperties = costRules[i].Split(_ruleSeparators, StringSplitOptions.RemoveEmptyEntries);
                rules[i].tags.tags = ruleProperties[0].Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
                rules[i].value = float.Parse(ruleProperties[1], CultureInfo.InvariantCulture);
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
                if (excludeTags[i].IsItemMatch(item))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetCost(ItemSign item)
        {
            float mul = 1;
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i].tags.IsItemMatch(item))
                {
                    mul *= rules[i].value;
                }
            }
            return Mathf.RoundToInt(item.BasicCost * mul);
        }
    }
    [CreateAssetMenu(menuName = "Configs/Shops")]
    public class ShopTable : Table<ShopSettings>
    {
        public override string TableName => "Shop";
        [SerializeField] private ShopSettings[] data;
        public override ShopSettings[] Data
        {
            get => data;
            protected set
            {
                data = value;
                foreach (var item in data)
                {
                    item.Postprocess();
                }
            }
        }
    }
}