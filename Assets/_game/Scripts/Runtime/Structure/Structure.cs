using System;
using System.Collections.Generic;
using System.Linq;
using Core.Game;
using Core.Graph;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
using Core.Utilities;
using Core.World;
using Runtime.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Runtime.Structure
{
    public class Structure : ItemObject, IStructure
    {
        [ShowInInspector] private HashSet<IBlock> _blocks = new();
        private List<Parent> _parents = new List<Parent>();
        private bool _isInitialized = false;
        private StructureGraph _graph = new StructureGraph();
        private BlocksConfiguration _configuration;
        public IGraph Graph => _graph;
        bool IStructure.Active => gameObject.activeSelf;
        Bounds IStructure.Bounds { get; } //TODO: constant updateing structure
        public IEnumerable<IBlock> Blocks => _blocks;
        public LateEvent OnInitComplete { get; } = new LateEvent();
        public List<Parent> Parents => _parents;
        
        public float Radius { get; private set; }
        
        protected virtual void Awake()
        {
            _isInitialized = false;
            this.AddWorldOffsetAnchor();
        }

        [Button]
        private void Init()
        {
            Init(true);
        }

        public virtual void Init(bool force = false)
        {
            if (_isInitialized && !force)
            {
                return;
            }
            _parents.Add(new Parent(transform, this));
            _graph.Init();
            RefreshBlocks();
            CalculateStructureRadius();
            OnInitComplete.Invoke();
            _isInitialized = true;
        }

        protected virtual void OnDestroy()
        {
            _graph?.Dispose();
        }

        public void SetConfiguration(BlocksConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddBlock(IBlock block)
        {
            var parent = this.GetParentFor(block);
            if (parent == null)
            {
                parent = new Parent(block.transform.parent, this);
                _parents.Add(parent);
            }
            
            parent.AddBlock(block);
            _configuration.SetupBlock(block, this, parent);
            block.InitBlock(this, parent);
            _blocks.Add(block);
            if (block is IGraphNode node)
            {
                _graph.AddNode(node);
            }
            OnBlockAddedEvent?.Invoke(block);
        }

        public void RemoveBlock(IBlock block)
        {
            block.Remove();
            _blocks.Remove(block);
            block.Parent.RemoveBlock(block);
            if (block is IGraphNode node)
            {
                _graph.RemoveNode(node);
            }
            OnBlockRemovedEvent?.Invoke(block);
        }

        public Parent GetOrFindParent(string path)
        {
            foreach (var p in Parents)
            {
                if (p.IsPatchMatch(path))
                {
                    return p;
                }
            }
            
            var parent = transform.FindDeepChild(path);
            if (parent)
            {
                var result = new Parent(parent, this);
                Parents.Add(result);
                return result;
            }

            return null;
        }

        public event Action<IBlock> OnBlockAddedEvent;
        public event Action<IBlock> OnBlockRemovedEvent;

        private void RefreshBlocks()
        {
            IBlock[] childrenBlocks = gameObject.GetComponentsInChildren<IBlock>();
            IEnumerable<IBlock> blocksToAdd = childrenBlocks.Except(_blocks);
            IEnumerable<IBlock> blocksToRemove = _blocks.Except(childrenBlocks);
            List<IBlock> cache = new List<IBlock>(childrenBlocks.Length + _blocks.Count);
            foreach (var block in blocksToAdd)
            {
                cache.Add(block);
            }
            int addRange = cache.Count;
            foreach (var block in blocksToRemove)
            {
                cache.Add(block);
            }

            for (int i = 0; i < addRange; i++)
            {
                AddBlock(cache[i]);
            }
            for (int i = addRange; i < cache.Count; i++)
            {
                RemoveBlock(cache[i]);
            }
        }


        private void CalculateStructureRadius()
        {
            Bounds allB = new Bounds(transform.position, Vector3.zero);
            foreach (IUpdatableBlock block in _blocks.OfType<IUpdatableBlock>())
            {
                allB.Encapsulate(block.GetBounds());
            }
            Radius = allB.extents.sqrMagnitude;
        }

        public Dictionary<string, List<(object owner, Action action)>> Events { get; } = new();
        public Dictionary<string, List<(object owner, ActionBox action)>> EventsT { get; } = new();
    }
}
