using System.Collections;
using System.Collections.Generic;
using Structure.Rigging;
using Structure.Wires;
using UnityEngine;

namespace Structure
{
    public interface IStructure
    {
        Transform transform { get; }
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
        
        string Configuration { get; set; }
        //TODO: Navigation

        void InitBlocks();
        void RefreshParents();
        void InitWires();
    }

    public interface IDynamicStructure : IStructure
    {
        float Mass { get; }
        Vector3 Velocity { get; }

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
            foreach (var block in structure.Blocks)
            {
                if(block.transform.parent == transform) Blocks.Add(block);
            }
        }
    }
}