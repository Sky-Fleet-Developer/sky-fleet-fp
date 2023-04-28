using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Game;
using Core.Graph;
using Core.Graph.Wires;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Core.Utilities;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Structure
{
    public abstract class BaseStructure : MonoBehaviour, IStructure
    {
        [ShowInInspector]
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid))
                {
                    guid = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                 UnityEditor.EditorUtility.SetDirty(this);   
#endif
                }
                return guid;
            }
            set => guid = value;
        }
        [SerializeField, HideInInspector] private string guid;

        public List<string> Tags => tags;
        [SerializeField] private List<string> tags;
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
        protected List<Parent> parents = null;
        //protected StructureConfiguration currentConfiguration;
        private bool initialized = false;

        private Dictionary<System.Type, IBlock[]> blocksCache;


        protected virtual void Awake()
        {
            initialized = false;
            this.AddWorldOffsetAnchor();
        }

        public virtual void Init()
        {
            RefreshBlocksAndParents();
            InitBlocks();
            CalculateStructureRadius();
            OnInitComplete.Invoke();
            StructureUpdateModule.RegisterStructure(this);
            initialized = true;
        }

        protected void OnDestroy()
        {
            StructureUpdateModule.DestroyStructure(this);
        }

        [Button]
        public void InitBlocks()
        {
            foreach (IBlock block in Blocks)
            {
                block.InitBlock(this, this.GetParentFor(block));
            }
        }

        private void ClearBlocksCache()
        {
            blocksCache = null;
        }
        
        public void RefreshBlocksAndParents()
        {
            RefreshBlocks();
            InitParents();
        }
        
        public void RefreshBlocks()
        {
            ClearBlocksCache();
            Blocks = gameObject.GetComponentsInChildren<IBlock>().ToList();
        }

        public void InitParents()
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


        public T[] GetBlocksByType<T>() where T : IBlock
        {
            if (blocksCache == null) blocksCache = new Dictionary<System.Type, IBlock[]>();
            System.Type type = typeof(T);
            if (blocksCache.TryGetValue(type, out IBlock[] val)) return val as T[];

            List<T> selection = new List<T>();
            for (int i = 0; i < Blocks.Count; i++)
            {
                if (Blocks[i] is T block)
                {
                    selection.Add(block);
                }
            }

            T[] arr = selection.ToArray();
            blocksCache.Add(type, arr as IBlock[]);
            return arr;
        }
        
        public void CalculateStructureRadius()
        {
            Bounds allB = new Bounds(transform.position, Vector3.zero);
            foreach (IBlock block in GetBlocksByType<IUpdatableBlock>())
            {
                allB.Encapsulate(block.GetBounds());
            }
            Radius = allB.extents.sqrMagnitude;
        }
    }
}
