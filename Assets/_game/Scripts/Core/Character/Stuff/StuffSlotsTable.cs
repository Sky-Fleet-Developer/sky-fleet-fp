using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Configurations.GoogleSheets;
using Core.Misc;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Core.Character.Stuff
{
    [Serializable]
    public class SlotPreset
    {
        private readonly char[] _combinationSeparators = new[] { '&', ' ' };
        [SerializeField] public string presetId;
        [SerializeField] public string slotId; 
        [SerializeField] public float maxCapacity;
        private string[] properties;
        private string[] includeItemsTags;
        private string[] excludeItemsTags;
        [SerializeField] private TagCombination[] includeTags;
        [SerializeField] private TagCombination[] excludeTags;
        [SerializeField] private Property[] m_Properties;

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

            if (properties != null)
            {
                m_Properties = new Property[properties.Length];
                for (var i = 0; i < properties.Length; i++)
                {
                    if (Property.TryParse(properties[i], out Property property))
                    {
                        if (property.values.Length == 0)
                        {
                            continue;
                        }
                        m_Properties[i] = property;
                    }
                }
            }
            else
            {
                m_Properties = Array.Empty<Property>();
            }
        }

        public SlotCell ConvertToCell()
        {
            return new SlotCell(slotId, includeTags, excludeTags, maxCapacity, m_Properties);
        }
    }

    public abstract class StuffSlotsLocalSource : ScriptableObject
    {
        public abstract bool TryGetGridSource(string gridId, out SlotsGrid result);
    }
    [CreateAssetMenu(menuName = "SF/Configs/StuffSlots")]
    public class StuffSlotsTable : Table<SlotPreset>
    {
        [Inject] private DiContainer _diContainer;
        public override string TableName => "StuffSlots";
        [SerializeField] private SlotPreset[] data;
        [SerializeField] private StuffSlotsLocalSource[] localSources;
        private Dictionary<string, SlotsGrid> _gridPresets = new ();
        public const string GridIdentifierKey = "_g:";

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

        public bool TryCreateGrid(string inventoryKey, out SlotsGrid result)
        {
            int gridIdentifierIndex = inventoryKey.IndexOf(GridIdentifierKey, StringComparison.Ordinal);
            if (gridIdentifierIndex == -1) // not a grid
            {
                result = null;
                return false;
            } 
            
            string gridId = inventoryKey.Substring(gridIdentifierIndex + GridIdentifierKey.Length);

            if (!_gridPresets.TryGetValue(gridId, out var preset))
            {
                foreach (var localSource in localSources)
                {
                    if (localSource.TryGetGridSource(gridId, out preset))
                    {
                        break;
                    }
                }

                if (preset == null)
                {
                    List<SlotCell> cells = new();
                    foreach (var item in data) // search matching by presetId in all rows to collect entire grid
                    {
                        if (item.presetId.Equals(gridId))
                        {
                            cells.Add(item.ConvertToCell());
                        }
                    }
                    preset = new SlotsGrid(gridId, cells.ToArray());
                }

                _gridPresets.Add(gridId, preset);
            }
            result = (SlotsGrid)preset.Clone();
            _diContainer.Inject(result);
            result.SetAsInventory(inventoryKey);
            return true;
        }
    }
}