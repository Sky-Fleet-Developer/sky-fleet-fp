using System.Collections;
using System.Collections.Generic;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.Structure
{
    public interface IStructure : ITablePrefab, IWiresMaster
    {
        bool enabled { get; }
        bool Active { get; }
        // ReSharper disable once InconsistentNaming
        Vector3 position { get; }
        // ReSharper disable once InconsistentNaming
        Quaternion rotation { get; }
        Bounds Bounds { get; }
        float Radius { get; }
        //TODO: Visibility
        List<Parent> Parents { get; }
        List<IBlock> Blocks { get; }
        List<T> GetBlocksByType<T>();
        Coroutine StartCoroutine(IEnumerator routine);
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
        /// call this after all blocks was initialized to setup blocks interaction
        /// </summary>
        void OnInitComplete();
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