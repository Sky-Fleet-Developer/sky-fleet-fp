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
        T[] GetBlocksByType<T>() where T : IBlock;
        LateEvent OnInitComplete { get; }
        //TODO: Navigation

        /// <summary>
        /// make structure ready to work in runtime
        /// </summary>
        void Init();
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
    }
}