using System.Collections.Generic;
using System.Linq;
using Core.ContentSerializer.Bundles;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class State
    {
        public UnityEngine.Vector3 playerPos;
        public UnityEngine.Vector3 playerRot;

        public List<StructureBundle> structuresCache;
        //TODO: characters
        //TODO: session settings

        public State()
        {
            
        }
        
        public State(List<StructureBundle> structuresCache)
        {
            this.structuresCache = structuresCache;
            //TODO копировать настройки сессии и сохранить список модов (путей к модам)
        }
    }
    //public Dictionary<int, ModPointer> remap;
    /*[System.Serializable]
    public struct ModPointer
    {
        public int index;
        public string modPath;
    }*/
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class PlayerPropertyAttribute : System.Attribute { }

    [System.Serializable]
    public class BlockConfiguration
    {
        public string path;
        public string current;
        public Dictionary<string, string> setup;
    }

    [System.Serializable]
    public class StructureConfiguration
    {
        public List<BlockConfiguration> blocks = new List<BlockConfiguration>();
        public List<string> wires = new List<string>();
        
        private Dictionary<string, BlockConfiguration> blocksCache;
        
        public BlockConfiguration GetBlock(string path)
        {
            blocksCache ??= blocks.ToDictionary(block => block.path);

            blocksCache.TryGetValue(path, out BlockConfiguration value);
            
            return value;
        }
    }
}
