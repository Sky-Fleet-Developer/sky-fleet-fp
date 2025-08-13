using System;
using System.Collections;
using System.Collections.Generic;
using Core.Cargo;
using Core.Configurations;
using Core.Items;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public interface IStructure : IItemInstanceHandle, IEventSystem
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
}