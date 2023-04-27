using System;
using System.Collections;
using System.Collections.Generic;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public interface IStructure
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
        /* /// <summary>
        /// initialize all wires from current configuration
        /// </summary>
        void InitWires();*/

        void UpdateStructureLod(int lod, Vector3 cameraPos);

        void CalculateStructureRadius();
    }
}