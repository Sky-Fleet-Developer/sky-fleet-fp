using System.Collections.Generic;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure
{
    public interface ITablePrefab
    {
        Transform transform { get; }
        string Guid { get; }
        List<string> Tags { get; }
    }
    
    public interface IStructure : ITablePrefab
    {
        bool enabled { get; }
        bool Active { get; }
        // ReSharper disable once InconsistentNaming
        Vector3 position { get; }
        // ReSharper disable once InconsistentNaming
        Quaternion rotation { get; }
        Bounds Bounds { get; }
        //TODO: Visibility
        List<Parent> Parents { get; }
        List<IBlock> Blocks { get; }
        List<Wire> Wires { get; }
        List<T> GetBlocksByType<T>();
        Port GetPort(string id);

        
        string Configuration { get; set; }
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
        /// <summary>
        /// initialize all wires from current configuration
        /// </summary>
        void InitWires();
    }

    public interface IDynamicStructure : IStructure
    {
        float Mass { get; }
        Vector3 Velocity { get; }
        Vector3 GetVelocityForPoint(Vector3 worldPoint);

        void AddForce(Vector3 force, Vector3 position);
    }

    public class Parent
    {
        public Transform Transform { get; }
        // ReSharper disable once InconsistentNaming
        public List<IBlock> Blocks;
        //TODO: Navigation

        public Parent(Transform transform, IStructure structure)
        {
            Transform = transform;
            Blocks = new List<IBlock>();
            foreach (IBlock block in structure.Blocks)
            {
                if(block.transform.parent == transform) Blocks.Add(block);
            }
        }
    }
}