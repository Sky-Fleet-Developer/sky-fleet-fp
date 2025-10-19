using System.Collections.Generic;
using System.Linq;
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
            bounds = transform.GetBounds();
        }

        public void AddBlock(IBlock block)
        {
            Blocks.Add(block);
            mass += block.Mass;
        }

        public void RemoveBlock(IBlock block)
        {
            Blocks.Remove(block);
            mass -= block.Mass;
        }

        public IBlock GetOrFindBlock(string blockName)
        {
            if (Blocks.Count > 0)
            {
                return Blocks.FirstOrDefault(x => x.transform.name == blockName);
            }
            return Transform.Find(blockName)?.GetComponent<IBlock>();
        }

        public bool IsPatchMatch(string value)
        {
            if (value == path) return true;
            if (path.Length == 0)
            {
                if (value.Length == 0)
                {
                    return true;
                }

                return false;
            }
            if (path.Length - value.Length == 1 && (path[^1] == '\\' || path[^1] == '/'))
            {
                int matches = 0;
                int wantedMatches = value.Length;
                for (var i = 0; i < wantedMatches; i++)
                {
                    if (value[i] == path[i])
                    {
                        matches++;
                    }
                }

                return matches == wantedMatches;
            }
            return false;
        }
    }
}