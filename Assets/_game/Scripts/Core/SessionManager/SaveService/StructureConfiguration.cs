using System.Collections.Generic;
using System.Linq;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class StructureConfiguration
    {
        public List<BlockConfiguration> blocks = new List<BlockConfiguration>();
        public List<List<string>> wires = new List<List<string>>();
        
        private Dictionary<string, BlockConfiguration> blocksCache;
        
        public BlockConfiguration GetBlock(string path)
        {
            blocksCache ??= blocks.ToDictionary(block => block.path);

            blocksCache.TryGetValue(path, out BlockConfiguration value);
            
            return value;
        }
    }
}