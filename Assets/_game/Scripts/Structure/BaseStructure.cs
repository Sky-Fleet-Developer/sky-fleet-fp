using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Structure.Rigging;
using Structure.Wires;


namespace Structure
{
    public abstract class BaseStructure : MonoBehaviour, IStructure
    {
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

        [ShowInInspector] protected List<IBlock> blocks;

        public Transform[] parentsObjects;
        protected List<Parent> parents = null;
        [SerializeField] protected string configuration;
        protected StructureConfiguration currentConfiguration;
        [ShowInInspector] protected List<Wire> wires;

        protected virtual void Awake()
        {
            if (!string.IsNullOrEmpty(configuration))
            {
                _ = ApplyConfigurationAndRegister();
            }
            else
            {
                InitBlocks();
                OnInitComplete();
                StructureManager.RegisterStructure(this);
            }
        }
        
        private void Start()
        {
        }

        private async Task ApplyConfigurationAndRegister()
        {
            currentConfiguration = JsonConvert.DeserializeObject<StructureConfiguration>(configuration);
            await Factory.ApplyConfiguration(this, currentConfiguration);
            StructureManager.RegisterStructure(this);
        }

        protected void OnDestroy()
        {
            StructureManager.DestroyStructure(this);
        }

        [Button]
        public void InitBlocks()
        {
            RefreshBlocks();
            InitParents();

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
        public void RefreshParents()
        {
            RefreshBlocks();
            InitParents();
        }

        public void RefreshBlocks()
        {
            blocksHash = null;
            blocks = gameObject.GetComponentsInChildren<IBlock>().ToList();
        }

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

        private Dictionary<System.Type, object> blocksHash;

        public List<T> GetBlocksByType<T>()
        {
            if (blocksHash == null) blocksHash = new Dictionary<System.Type, object>();
            var type = typeof(T);
            if (blocksHash.TryGetValue(type, out object val)) return val as List<T>;

            List<T> selection = new List<T>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is T block)
                {
                    selection.Add(block);
                }
            }

            blocksHash.Add(type, selection);
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

        private Dictionary<string, Port> portsHash;

        public Port GetPort(string guid)
        {
            if (portsHash == null) portsHash = new Dictionary<string, Port>();
            if (portsHash.TryGetValue(guid, out Port port)) return port;

            var ports = Factory.GetAllPorts(this);
            port = ports.FirstOrDefault(x => x.Guid == guid);
            portsHash.Add(guid, port);
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
