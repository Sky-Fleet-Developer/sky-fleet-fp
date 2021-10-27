using System.Collections.Generic;
using System.Linq;
using Core.ContentSerializer.Bundles;
using UnityEngine;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class State
    {
        public Vector3 playerPos;
        public Vector3 playerRot;

        public List<StructureBundle> structuresCache;

        public State()
        {
            
        }
        
        public State(List<StructureBundle> structuresCache)
        {
            this.structuresCache = structuresCache;
        }
    }

    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class PlayerPropertyAttribute : System.Attribute { }

    [System.Serializable]
    public class BlockConfiguration
    {
        public string path; //путь к модолю по трансформам/парент
        public string currentGuid; // текущий гуид
        public Dictionary<string, string> setup; //свойства помеченные [PlayerProperty]
    }

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
