using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public interface IStructure : ITablePrefab
    {
        bool Active { get; }
        // ReSharper disable once InconsistentNaming
        Transform transform { get; }
        Bounds Bounds { get; }
        float Radius { get; }
        //TODO: Visibility
        List<Parent> Parents { get; }
        List<IBlock> Blocks { get; }
        LateEvent OnInitComplete { get; }
        //TODO: Navigation

        /// <summary>
        /// make structure ready to work in runtime
        /// </summary>
        void Init(bool force = false);
        /// <summary>
        /// init current blocks from current structure
        /// </summary>
        void InitBlocks();
        /// <summary>
        /// check new blocks and parents in hierarchy and remove deleted items
        /// </summary>
        void RefreshBlocksAndParents();
    }

    public static class StructureExtension
    {
        public static IBlock GetBlockByPath(this IStructure structure, string path, string blockName)
        {
            if (path.Length == 0)
            {
                return structure.Blocks.FirstOrDefault(x => x.transform.name == blockName);
            }

            for (int i = 0; i < structure.Parents.Count; i++)
            {
                if (structure.Parents[i].Path == path)
                {
                    return structure.Parents[i].Blocks.FirstOrDefault(x => x.transform.name == blockName);
                }
            }

            return null;
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

        private static Dictionary<IStructure, Dictionary<System.Type, IBlock[]>> blocksCache =
            new Dictionary<IStructure, Dictionary<Type, IBlock[]>>();


        public static void AddBlocksCache(this IStructure structure)
        {
            blocksCache[structure] = new Dictionary<Type, IBlock[]>();
        }

        public static void RemoveBlocksCache(this IStructure structure)
        {
            blocksCache.Remove(structure);
        }

        public static void TryClearBlocksCache(this IStructure structure)
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
            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                if (structure.Blocks[i] is T block)
                {
                    selection.Add(block);
                }
            }

            T[] arr = selection.ToArray();
            blocksCache[structure].Add(type, arr as IBlock[]);
            return arr;
        }
    }
}