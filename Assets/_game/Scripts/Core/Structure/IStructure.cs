using System;
using System.Collections;
using System.Collections.Generic;
using Core.Cargo;
using Core.Configurations;
using Core.Graph;
using Core.Items;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public interface IStructure : IItemObjectHandle, IEventSystem
    {
        bool Active { get; }
        // ReSharper disable once InconsistentNaming
        Transform transform { get; }
        Bounds Bounds { get; }
        float Radius { get; }
        //TODO: Visibility
        List<Parent> Parents { get; }
        IEnumerable<IBlock> Blocks { get; }
        IGraph Graph { get; }
        LateEvent OnInitComplete { get; }
        //TODO: Navigation

        /// <summary>
        /// make structure ready to work in runtime
        /// </summary>
        void Init(bool force = false);
        void SetConfiguration(BlocksConfiguration configuration);
        void AddBlock(IBlock block);
        void RemoveBlock(IBlock block);
        Parent GetOrFindParent(string path);
        event Action<IBlock> OnBlockAddedEvent;
        event Action<IBlock> OnBlockRemovedEvent;
    }
}