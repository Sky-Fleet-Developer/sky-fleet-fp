using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging;
using Core.World;
using UnityEngine;

namespace Core.Structure.Serialization
{
    [System.Serializable]
    public class BlocksConfiguration : Configuration<IStructure>
    {
        public List<BlockConfiguration> blocks = new List<BlockConfiguration>();

        private Dictionary<string, BlockConfiguration> blocksCache;

        public BlocksConfiguration() : base(){}
        public BlocksConfiguration(IStructure structure) : base(structure)
        {
            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                IBlock block = structure.Blocks[i];

                blocks.Add(new BlockConfiguration(block));
            }
        }
        
        public BlockConfiguration GetBlock(string path, string blockName)
        {
            blocksCache ??= blocks.ToDictionary(block => $"{block.path}.{block.blockName}");

            blocksCache.TryGetValue($"{path}.{blockName}", out BlockConfiguration value);
            
            return value;
        }
        
        public override async Task Apply(IStructure structure)
        {
            if (structure.transform.gameObject.activeInHierarchy == false)
            {
                Debug.LogError("Apply configuration only to instances!");
                return;
            }
            
            structure.RefreshBlocksAndParents();

            List<Task> waiting = new List<Task>();
            
            foreach (BlockConfiguration blockConfiguration in blocks)
            {
                waiting.Add(blockConfiguration.ApplyConfiguration(structure));
            }
            
            await Task.WhenAll(waiting);

            
            await Task.Yield();
            structure.RefreshBlocksAndParents();
            structure.InitBlocks();

            foreach (IBlock block in structure.Blocks)
            {
                string path = block.GetPath();
                BlockConfiguration blockConfig = GetBlock(path, block.transform.name);
                blockConfig?.ApplySetup(block);
            }

            try
            {
                structure.InitBlocks();
                Debug.Log($"{structure.transform.name} configuration success!");
            }
            catch (Exception e)
            {
                Debug.LogError("Error when init structure: " + e);
            }
        }
    }
}