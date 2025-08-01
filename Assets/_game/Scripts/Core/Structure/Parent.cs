﻿using System.Collections.Generic;
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

        public bool IsPatchMatch(string value)
        {
            if (value == path) return true;
            if (path.Length - value.Length == 1 && path[^1] == '\\' || path[^1] == '/')
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