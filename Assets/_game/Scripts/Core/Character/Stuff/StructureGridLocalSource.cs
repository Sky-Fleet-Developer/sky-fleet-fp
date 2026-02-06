using System;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Structure.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Character.Stuff
{
    [CreateAssetMenu(menuName = "SF/Configs/StructureGridLocalSource")]
    public class StructureGridLocalSource : StuffSlotsLocalSource
    {
        [Serializable]
        private class StructureGrid
        {
            public string id;
            public BlockCell[] cells;

            public SlotsGrid ConvertToGrid()
            {
                return new(id, cells.Select(c => c.ConvertToSlotCell()).ToArray());
            }
        }
        [Serializable]
        private class BlockCell
        {
            public string slotId;
            public string path;
            public int sibling;
            public Vector3 position;
            public Vector3 rotation;
            public string mountingType;
            public string[] constantFieldKeys;
            public string[] constantFieldValues;
            
            public SlotCell ConvertToSlotCell()
            {
                var properties = new Property[5]
                {
                    new()
                    {
                        name = Property.PositionPropertyName,
                        values = new PropertyValue[] { new(position.x), new(position.y), new(position.z) }
                    },
                    new()
                    {
                        name = Property.RotationPropertyName,
                        values = new PropertyValue[] { new(rotation.x), new(rotation.y), new(rotation.z) }
                    },
                    new() { name = Property.SiblingPropertyName, values = new PropertyValue[] { new(sibling) } },
                    new() { name = Property.PathPropertyName, values = new PropertyValue[] { new(path) } },
                    default
                };

                if (constantFieldKeys.Length > 0)
                {
                    Property constantFieldsProperty = new Property(name: Property.ConstantFieldsPropertyName, new PropertyValue[constantFieldKeys.Length]);
                    
                    for (var i = 0; i < constantFieldKeys.Length; i++)
                    {
                        constantFieldsProperty.values[i] = new PropertyValue($"{constantFieldKeys[i]}:{constantFieldValues[i]}");
                    }

                    properties[4] = constantFieldsProperty;
                }

                return new SlotCell(slotId, 
                    string.IsNullOrEmpty(mountingType) ? 
                        new TagCombination[1]
                    {
                        new ()
                        {
                            tags = new[]
                            {
                                $"{ItemSign.BlockTag}"
                            }
                        }
                    }
                        : new TagCombination[1]
                    {
                        new ()
                        {
                            tags = new[]
                            {
                                $"{ItemSign.MountingTag}:{mountingType}", $"{ItemSign.BlockTag}"
                            }
                        }
                    }, 
                    Array.Empty<TagCombination>(),
                    1,
                    properties);
            }
        }
        
        [SerializeField] private StructureGrid[] data;
        private Dictionary<string, SlotsGrid> _cache;
        public override bool TryGetGridSource(string gridId, out SlotsGrid result)
        {
            _cache??= data.ToDictionary(k => k.id, v => v.ConvertToGrid());
            return _cache.TryGetValue(gridId, out result);
        }

        [Button]
        private void AddFromBlocksConfig(string id, BlocksConfiguration blocksConfig)
        {
            Array.Resize(ref data, data.Length + 1);
            data[^1] = new StructureGrid(){id = id, cells = blocksConfig.blocks.Select(
                v => new BlockCell 
                { 
                    slotId = v.blockName,
                    mountingType = "", 
                    path = v.path, 
                    sibling = v.sibilingIdx,
                    position = v.localPosition,
                    rotation = v.localRotation,
                    constantFieldKeys = v.constantFieldsKeys.ToArray(),
                    constantFieldValues = v.constantFieldsValues.ToArray()
                }).ToArray()};
        }
    }
}