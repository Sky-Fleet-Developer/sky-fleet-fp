using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Core.Structure.Wires;
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
        IEnumerable<Wire> IWiresMaster.Wires => wires;
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

        public float Radius { get; private set; }

        [SerializeField, HideInInspector] private string guid;

        [ShowInInspector] protected List<IBlock> blocks;

        public Transform[] parentsObjects;
        protected List<Parent> parents = null;
        [SerializeField] protected string configuration;
        protected StructureConfiguration currentConfiguration;
        [ShowInInspector] protected List<Wire> wires;
        
        private bool initialized = false;
        
        private Dictionary<string, PortPointer> portsCache;
        private List<PortPointer> portsPointersCache;
        private Dictionary<System.Type, object> blocksCache;
       

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
                OnFinishInit();
            }

            initialized = true;
        }

        protected virtual void OnFinishInit()
        {
            CalculateStructureRadius();
        }

        private async Task ApplyConfigurationAndRegister()
        {
            currentConfiguration = JsonConvert.DeserializeObject<StructureConfiguration>(configuration);
            await Factory.ApplyConfiguration(this, currentConfiguration);
            StructureUpdateModule.RegisterStructure(this);
            OnFinishInit();
        }

        protected void OnDestroy()
        {
            StructureUpdateModule.DestroyStructure(this);
        }

        [Button]
        public void InitBlocks()
        {
            foreach (IBlock block in blocks)
            {
                block.InitBlock(this, GetParentFor(block));
            }
        }

        public void OnInitComplete()
        {
            foreach (IBlock block in blocks)
            {
                block.OnInitComplete();
            }
        }
        
        private void ClearBlocksCache()
        {
            portsPointersCache = null;
            portsCache = null;
            blocksCache = null;
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
            ClearBlocksCache();
            blocks = gameObject.GetComponentsInChildren<IBlock>().ToList();
        }

        [Button]
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


        public List<T> GetBlocksByType<T>()
        {
            if (blocksCache == null) blocksCache = new Dictionary<System.Type, object>();
            System.Type type = typeof(T);
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


        
        public PortPointer GetPort(string id)
        {
            if (portsCache == null) portsCache = new Dictionary<string, PortPointer>();
            if (portsCache.TryGetValue(id, out PortPointer port)) return port;

            if(portsPointersCache == null) portsPointersCache = Factory.GetAllPorts(this);
            
            port = portsPointersCache.FirstOrDefault(x => x.Equals(id));
            portsCache.Add(id, port);
            return port;
        }

        public void ConnectPorts(params PortPointer[] ports)
        {
            Wire existWire = null;
            
            foreach (PortPointer port in ports)
            {
                existWire = port.Port.GetWire();
                if (existWire != null) break;
            }

            if (existWire == null) Wires.Utilities.CreateWireForPorts(this, ports);
            else Wires.Utilities.AddPortsToWire(existWire, ports);
        }

        void IWiresMaster.AddWire(Wire wire)
        {
            wires.Add(wire);
        } 

        [Button]
        public void InitWires()
        {
            wires = new List<Wire>();
            if (!string.IsNullOrEmpty(configuration))
            {
                currentConfiguration = JsonConvert.DeserializeObject<StructureConfiguration>(configuration);
                if (currentConfiguration == null) return;
            }
            
            foreach (List<string> wire in currentConfiguration.wires)
            {
                PortPointer[] portsToConnect = new PortPointer[wire.Count];
                
                for (var i = 0; i < wire.Count; i++)
                {
                    portsToConnect[i] = GetPort(wire[i]);
                }

                ConnectPorts(portsToConnect);
            }
        }

        public virtual void UpdateStructureLod(int lod, Vector3 cameraPos)
        {
            foreach(IUpdatableBlock block in GetBlocksByType<IUpdatableBlock>())
            {
                block.UpdateBlock(lod);
            }
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
