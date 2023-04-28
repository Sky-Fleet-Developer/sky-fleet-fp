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

        // ReSharper disable once InconsistentNaming
        public List<IBlock> Blocks;
        //TODO: Navigation

        public Parent(Transform transform, IStructure structure)
        {
            Transform = transform;
            path = transform.GetPath(structure.transform);
            Blocks = new List<IBlock>();
            foreach (IBlock block in structure.Blocks)
            {
                if(block.transform.parent == transform) Blocks.Add(block);
            }
        }
        

    }
}