using System;
using System.Collections.Generic;
using System.Linq;
using Core.Game;
using Core.Items;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Runtime.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure
{
    public class Structure : ItemInstance, IStructure
    {
        bool IStructure.Active => gameObject.activeSelf;

        Bounds IStructure.Bounds { get; } //TODO: constant updateing structure
        public LateEvent OnInitComplete { get; } = new LateEvent();
        public List<Parent> Parents
        {
            get
            {
                if (parents == null)
                {
                    InitParents();
                }

                return parents;
            }
        }
        
        public float Radius { get; private set; }

        [ShowInInspector] public List<IBlock> Blocks { get; private set; }

        public Transform[] parentsObjects;

        private List<Parent> parents = null;
        private bool initialized = false;
        
        private ItemSign _sourceItem;
        ItemSign IItemInstance.SourceItem => _sourceItem;
        void IItemInstanceHandle.SetSourceItem(ItemSign sign)
        {
            _sourceItem = sign;
        }
        
        protected virtual void Awake()
        {
            initialized = false;
            this.AddWorldOffsetAnchor();
        }

        [Button]
        private void Init()
        {
            Init(true);
        }

        public virtual void Init(bool force = false)
        {
            if (initialized && !force)
            {
                return;
            }
            RefreshBlocksAndParents();
            InitBlocks();
            this.AddBlocksCache();
            CalculateStructureRadius();
            OnInitComplete.Invoke();
            StructureUpdateModule.RegisterStructure(this);
            initialized = true;
        }

        protected void OnDestroy()
        {
            StructureUpdateModule.DestroyStructure(this);
            this.RemoveBlocksCache();
        }

        [Button]
        public void InitBlocks()
        {
            foreach (IBlock block in Blocks)
            {
                block.InitBlock(this, this.GetParentFor(block));
            }
        }
        
        public void RefreshBlocksAndParents()
        {
            RefreshBlocks();
            InitParents();
        }

        private void RefreshBlocks()
        {
            this.TryClearBlocksCache();
            Blocks = gameObject.GetComponentsInChildren<IBlock>().ToList();
        }

        private void InitParents()
        {
            if (parentsObjects != null)
            {
                parents = new List<Parent>(parentsObjects.Length + 1);
                foreach (Transform parentsObject in parentsObjects)
                {
                    parents.Add(new Parent(parentsObject, this));
                }

                parents.Add(new Parent(transform, this));
            }
        }


        private void CalculateStructureRadius()
        {
            Bounds allB = new Bounds(transform.position, Vector3.zero);
            foreach (IUpdatableBlock block in this.GetBlocksByType<IUpdatableBlock>())
            {
                allB.Encapsulate(block.GetBounds());
            }
            Radius = allB.extents.sqrMagnitude;
        }

        public Dictionary<string, List<(object owner, Action action)>> Events { get; } = new();
        public Dictionary<string, List<(object owner, ActionBox action)>> EventsT { get; } = new();
    }
}
