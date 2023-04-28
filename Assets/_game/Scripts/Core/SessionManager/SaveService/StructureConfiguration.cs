using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class StructureConfiguration
    {
        public string bodyGuid;
        public List<BlockConfiguration> blocks = new List<BlockConfiguration>();
        public List<WireConfiguration> wires = new List<WireConfiguration>();
        
        private Dictionary<string, BlockConfiguration> blocksCache;
        
        public BlockConfiguration GetBlock(string path, string blockName)
        {
            blocksCache ??= blocks.ToDictionary(block => $"{block.path}.{block.blockName}");

            blocksCache.TryGetValue($"{path}.{blockName}", out BlockConfiguration value);
            
            return value;
        }

        public void ApplyWires(IGraph graph)
        {
            foreach (WireConfiguration wire in wires)
            {
                PortPointer[] portsToConnect = new PortPointer[wire.ports.Count];

                for (var i = 0; i < wire.ports.Count; i++)
                {
                    portsToConnect[i] = graph.GetPort(wire.ports[i]);
                }

                graph.ConnectPorts(portsToConnect);
            }
        }
        
        public async Task ApplyConfiguration(BaseStructure structure)
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
                waiting.Add(blockConfiguration.Instantiate(structure));
            }
            
            await Task.WhenAll(waiting);

            
            await Task.Yield();
            structure.RefreshBlocksAndParents();
            structure.InitBlocks();

            foreach (IBlock block in structure.Blocks)
            {
                string path = Factory.GetPath(block);
                BlockConfiguration blockConfig = GetBlock(path, block.transform.name);
                blockConfig?.ApplySetup(block);
            }

            try
            {
                structure.InitBlocks();
                
                //structure.OnInitComplete.Invoke();
                Debug.Log($"{structure.transform.name} configuration success!");
            }
            catch (Exception e)
            {
                Debug.LogError("Error when init structure: " + e);
            }
        }
    }

    [System.Serializable]
    public class WireConfiguration
    {
        public List<string> ports;

        public WireConfiguration(List<string> ports)
        {
            this.ports = ports;
        }
    }
}