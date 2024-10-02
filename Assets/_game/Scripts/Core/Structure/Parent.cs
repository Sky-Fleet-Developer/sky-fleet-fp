using System.Collections.Generic;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public class Parent
    {
        public Transform Transform { get; }
        public string Path => path;
        private string path;
        private float mass;
        public float Mass => mass;
        private Bounds bounds;
        public Bounds Bounds => bounds;

        // ReSharper disable once InconsistentNaming
        public List<IBlock> Blocks;
        //TODO: Navigation

        public Parent(Transform transform, IStructure structure)
        {
            Transform = transform;
            path = transform.GetPath(structure.transform);
            Blocks = new List<IBlock>();
            mass = 0;
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (structure.Blocks == null)
            {
                return;
            }
            foreach (IBlock block in structure.Blocks)
            {
                if (block.transform.parent == transform)
                {
                    Blocks.Add(block);
                    mass += block.Mass;
                    bounds.Encapsulate(block.GetBounds());
                }
            }
        }

    }
}