using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Configurations.GoogleSheets;
using UnityEngine;

namespace Core.Character.Stuff
{
    [Serializable]
    public class SlotPreset
    {
        private readonly char[] _combinationSeparators = new[] { '&', ' ' };
        [SerializeField] public string presetId;
        [SerializeField] public string slotId;
        private string[] includeItemsTags;
        private string[] excludeItemsTags;
        [SerializeField] private TagCombination[] includeTags;
        [SerializeField] private TagCombination[] excludeTags;

        public void Postprocess()
        {
            if (includeItemsTags != null)
            {
                includeTags = new TagCombination[includeItemsTags.Length];
                for (var i = 0; i < includeTags.Length; i++)
                {
                    includeTags[i].tags = includeItemsTags[i]
                        .Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            else
            {
                includeTags = Array.Empty<TagCombination>();
            }

            if (excludeItemsTags != null)
            {
                excludeTags = new TagCombination[excludeItemsTags.Length];
                for (var i = 0; i < excludeTags.Length; i++)
                {
                    excludeTags[i].tags = excludeItemsTags[i]
                        .Split(_combinationSeparators, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            else
            {
                excludeTags = Array.Empty<TagCombination>();
            }
        }

        public SlotCell ConvertToCell()
        {
            return new SlotCell(slotId, includeTags, excludeTags);
        }
    }
    [CreateAssetMenu(menuName = "SF/Configs/StuffSlots")]
    public class StuffSlotsTable : Table<SlotPreset>
    {
        public override string TableName => "StuffSlots";
        [SerializeField] private SlotPreset[] data;
        private Dictionary<string, SlotsGrid> _gridPresets = new ();

        protected override SlotPreset[] Data
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

        public SlotsGrid CreateGrid(string inventoryKey, string gridId)
        {
            if (!_gridPresets.TryGetValue(gridId, out var preset))
            {
                List<SlotCell> cells = new();
                foreach (var item in data)
                {
                    if (item.presetId.Equals(gridId))
                    {
                        cells.Add(item.ConvertToCell());
                    }
                }
                preset = new SlotsGrid(gridId, cells.ToArray());
                _gridPresets.Add(gridId, preset);
            }
            var result = (SlotsGrid)preset.Clone();
            result.SetAsInventory(inventoryKey);
            return result;
        }
    }
}