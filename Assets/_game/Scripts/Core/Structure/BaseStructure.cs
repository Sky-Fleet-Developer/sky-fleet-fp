using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure
{
    public abstract class BaseStructure : MonoBehaviour, IStructure
    {
        [ShowInInspector]
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
                return guid;
            }
            set => guid = value;
        }

        public List<string> Tags => tags;
        [SerializeField] private List<string> tags;
        public Vector3 position => transform.position;
        public Quaternion rotation => transform.rotation;
        List<IBlock> IStructure.Blocks => blocks;
        List<Wire> IStructure.Wires => wires;
        bool IStructure.Active => gameObject.activeSelf;

        Bounds IStructure.Bounds { get; } //TODO: constant updateing structure

        List<Parent> IStructure.Parents
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

        public string Configuration
        {
            get => configuration;
            set => configuration = value;
        }
        
        [SerializeField, HideInInspector] private string guid;

        [ShowInInspector] protected List<IBlock> blocks;

        public Transform[] parentsObjects;
        protected List<Parent> parents = null;
        [SerializeField] protected string configuration;
        protected StructureConfiguration currentConfiguration;
        [ShowInInspector] protected List<Wire> wires;
        
        private bool initialized = false;

        protected virtual void Awake()
        {
            initialized = false;
        }
        
        protected virtual void Start()
        {
            if (!initialized) Init();
        }

        public void Init()
        {
            if (!string.IsNullOrEmpty(configuration))
            {
                _ = ApplyConfigurationAndRegister();
            }
            else
            {
                RefreshBlocks();
                InitParents();
                InitBlocks();
                OnInitComplete();
                StructureUpdateModule.RegisterStructure(this);
            }

            initialized = true;
        }

        private async Task ApplyConfigurationAndRegister()
        {
            currentConfiguration = JsonConvert.DeserializeObject<StructureConfiguration>(configuration);
            await Factory.ApplyConfiguration(this, currentConfiguration);
            StructureUpdateModule.RegisterStructure(this);
        }

        protected void OnDestroy()
        {
            StructureUpdateModule.DestroyStructure(this);
        }

        [Button]
        public void InitBlocks()
        {
            foreach (var block in blocks)
            {
                block.InitBlock(this, GetParentFor(block));
            }
        }

        public void OnInitComplete()
        {
            foreach (var block in blocks)
            {
                block.OnInitComplete();
            }
        }
        
        [Button]
        public void RefreshBlocksAndParents()
        {
            RefreshBlocks();
            InitParents();
        }

        [Button]
        public void RefreshBlocks()
        {
            blocksCache = null;
            blocks = gameObject.GetComponentsInChildren<IBlock>().ToList();
        }

        [Button]
        public void InitParents()
        {
            if (parentsObjects != null)
            {
                parents = new List<Parent>(parentsObjects.Length + 1);
                foreach (var parentsObject in parentsObjects)
                {
                    parents.Add(new Parent(parentsObject, this));
                }

                parents.Add(new Parent(transform, this));
            }
        }

        private Dictionary<System.Type, object> blocksCache;

        public List<T> GetBlocksByType<T>()
        {
            if (blocksCache == null) blocksCache = new Dictionary<System.Type, object>();
            var type = typeof(T);
            if (blocksCache.TryGetValue(type, out object val)) return val as List<T>;

            List<T> selection = new List<T>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is T block)
                {
                    selection.Add(block);
                }
            }

            blocksCache.Add(type, selection);
            return selection;
        }

        public Parent GetParentFor(IBlock block)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                if (block.transform.parent == parents[i].Transform)
                {
                    return parents[i];
                }
            }

            return null;
        }

        private Dictionary<string, Port> portsCache;

        public Port GetPort(string guid)
        {
            if (portsCache == null) portsCache = new Dictionary<string, Port>();
            if (portsCache.TryGetValue(guid, out Port port)) return port;

            var ports = Factory.GetAllPorts(this);
            port = ports.FirstOrDefault(x => x.Guid == guid);
            portsCache.Add(guid, port);
            return port;
        }

        [Button]
        public void InitWires()
        {
            if (wires == null) wires = new List<Wire>();
            foreach (var wireString in currentConfiguration.wires)
            {
                var guids = wireString.Split(new[] {'.'}, System.StringSplitOptions.RemoveEmptyEntries);

                var port = GetPort(guids[0]);

                var newWire = port.CreateWire();
                wires.Add(newWire);

                port.SetWire(newWire);

                for (int i = 1; i < guids.Length; i++)
                {
                    port = GetPort(guids[i]);
                    port.SetWire(newWire);
                }
            }
        }
    }
}
