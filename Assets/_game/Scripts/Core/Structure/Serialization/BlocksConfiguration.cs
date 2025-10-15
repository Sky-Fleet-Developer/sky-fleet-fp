using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Utilities;
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
            foreach (var block in structure.Blocks)
            {
                blocks.Add(new BlockConfiguration(block));
            }
        }
        
        public BlockConfiguration FindBlockConfig(string path, string blockName)
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
            
            structure.SetConfiguration(this);

            List<Task> waiting = new List<Task>();
            
            foreach (BlockConfiguration blockConfiguration in blocks)
            {
                waiting.Add(blockConfiguration.ApplyConfiguration(structure));
            }
            
            await Task.WhenAll(waiting);

            /*foreach (IBlock block in structure.Blocks)
            {
                string path = block.GetPath();
                BlockConfiguration blockConfig = FindBlockConfig(path, block.transform.name);
                blockConfig?.ApplySetup(block);
            }*/


            try
            {
                Debug.Log($"{structure.transform.name} configuration success!");
            }
            catch (Exception e)
            {
                Debug.LogError("Error when init structure: " + e);
            }
        }

        public void SetupBlock(IBlock block, IStructure structure, Parent parent)
        {
            BlockConfiguration blockConfig = FindBlockConfig(parent.Transform.GetPath(structure.transform), block.transform.name);
            blockConfig?.ApplySetup(block);
        }
    }
}