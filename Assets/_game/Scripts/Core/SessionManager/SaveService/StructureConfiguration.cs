using System.Collections.Generic;
using System.Linq;
using Core.Structure;
using Core.Structure.Wires;

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

        public void ApplyWires(IWiresMaster wiresMaster)
        {
            foreach (WireConfiguration wire in wires)
            {
                PortPointer[] portsToConnect = new PortPointer[wire.ports.Count];

                for (var i = 0; i < wire.ports.Count; i++)
                {
                    portsToConnect[i] = wiresMaster.GetPort(wire.ports[i]);
                }

                wiresMaster.ConnectPorts(portsToConnect);
            }
        }
    }

    [System.Serializable]
    public class WireConfiguration
    {
        public List<string> ports = new List<string>();

        public WireConfiguration(List<string> ports)
        {
            this.ports = ports;
        }
    }
}