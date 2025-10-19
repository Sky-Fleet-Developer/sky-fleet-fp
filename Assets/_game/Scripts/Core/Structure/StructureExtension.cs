using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utilities;

namespace Core.Structure
{
    public static class StructureExtension
    {
        public static IBlock GetBlockByPath(this IStructure structure, string path, string blockName)
        {
            if (path.Length == 0)
            {
                var block = structure.Blocks.FirstOrDefault(x => x.transform.name == blockName);
                if(block != null) return block;
                
                return structure.transform.Find(blockName)?.GetComponent<IBlock>();
            }

            Parent parent = structure.GetOrFindParent(path);
            return parent?.GetOrFindBlock(blockName);
        }
        
        public static Parent GetParentFor(this IStructure structure, IBlock block)
        {
            for (int i = 0; i < structure.Parents.Count; i++)
            {
                if (block.transform.parent == structure.Parents[i].Transform)
                {
                    return structure.Parents[i];
                }
            }

            return null;
        }

        public static IEnumerable<T> GetBlocksByType<T>(this IStructure structure) where T : IBlock
        {
            return structure.Blocks.OfType<T>();
        }

        /*private static Dictionary<IStructure, Dictionary<System.Type, IBlock[]>> blocksCache =
            new Dictionary<IStructure, Dictionary<Type, IBlock[]>>();


        public static void AddBlocksCache(this IStructure structure)
        {
            blocksCache[structure] = new Dictionary<Type, IBlock[]>();
        }

        public static void RemoveBlocksCache(this IStructure structure)
        {
            blocksCache.Remove(structure);
        }

        public static void EnsureCacheClear(this IStructure structure)
        {
            if (blocksCache.ContainsKey(structure))
            {
                blocksCache[structure].Clear();
            }
        }
        
        public static T[] GetBlocksByType<T>(this IStructure structure) where T : IBlock
        {
            System.Type type = typeof(T);
            if (blocksCache[structure].TryGetValue(type, out IBlock[] val)) return val as T[];

            List<T> selection = new List<T>();
            foreach (var block in structure.Blocks)
            {
                if (block is T blockT)
                {
                    selection.Add(blockT);
                }
            }

            T[] arr = selection.ToArray();
            blocksCache[structure].Add(type, arr as IBlock[]);
            return arr;
        }*/
    }
}