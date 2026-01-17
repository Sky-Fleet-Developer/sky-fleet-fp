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
            
            public SlotCell ConvertToSlotCell()
            {
                return new SlotCell(slotId, 
                    string.IsNullOrEmpty(mountingType) ? Array.Empty<TagCombination>() : new TagCombination[1]
                    {
                        new ()
                        {
                            tags = new[]
                            {
                                $"{ItemSign.MountingTag}:{mountingType}"
                            }
                        }
                    }, 
                    Array.Empty<TagCombination>(),
                    1,
                    new Property[]
                    {
                        new (){name = nameof(position), values = new PropertyValue[]{new (position.x), new (position.y), new (position.z)}},
                        new (){name = nameof(rotation), values = new PropertyValue[]{new (rotation.x), new (rotation.y), new (rotation.z)}},
                        new (){name = nameof(sibling), values = new PropertyValue[]{new (sibling)}},
                        new (){name = nameof(path), values = new PropertyValue[]{new (path)}},
                    });
            }
        }
        
        [SerializeField] private StructureGrid[] data;
        private Dictionary<string, SlotsGrid> _cache = new ();
        public override bool TryGetGridSource(string inventoryKey, string gridId, out SlotsGrid result)
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
                    sibling = v.sibilingIdx 
                }).ToArray()};
        }
    }
}