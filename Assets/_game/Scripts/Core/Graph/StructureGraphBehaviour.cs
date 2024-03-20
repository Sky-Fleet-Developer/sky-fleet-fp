using System;
using System.Collections.Generic;
using System.Linq;
using Core.Graph.Wires;
using Core.Structure;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Graph
{
    public class StructureGraphBehaviour : MonoBehaviour, IGraph
    {
        private IStructure structure;
        private Dictionary<string, PortPointer> portsCache;
        private List<PortPointer> portsPointersCache;
        public List<IGraphNode> nodes = new List<IGraphNode>();
        [ShowInInspector] public List<Wire> wires = new List<Wire>();

        public IEnumerable<IGraphNode> Nodes => nodes;
        public IEnumerable<Wire> Wires => wires;
        private bool _isInitialized = false;
        private void Awake()
        {
            structure ??= GetComponent<IStructure>();
            structure.OnInitComplete.Subscribe(() => InitGraph(true));
        }

        public void InitGraph(bool force = false)
        {
            if(_isInitialized && !force) return;
            
            structure ??= GetComponent<IStructure>();
            
            portsCache = new Dictionary<string, PortPointer>();
            if (structure == null)
            {
                Debug.LogError($"{transform.name} has no IStructure component but try to init structure graph!");
                return;
            }
            
            nodes.Clear();
            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                if (structure.Blocks[i] is IGraphNode node)
                {
                    nodes.Add(node);
                    node.InitNode(this);
                }
            }

            _isInitialized = true;
        }

        public void AddWire(Wire wire)
        {
            wires.Add(wire);
        }

        public PortPointer GetPort(string id)
        {
            if (portsCache.TryGetValue(id, out PortPointer port)) return port;

            portsPointersCache ??= GetAllPorts();

            port = portsPointersCache.FirstOrDefault(x => x.Id.Equals(id));
            if (!port.IsNull())
            {
                portsCache.Add(id, port);
            }
            return port;
        }

        public void ConnectPorts(params PortPointer[] ports)
        {
            Wire existWire = null;

            foreach (PortPointer port in ports)
            {
                if (port.Port != null)
                {
                    existWire = port.Port.GetWire();
                }
                if (existWire != null) break;
            }

            if (existWire == null) CreateWireForPorts(ports);
            else Graph.Wires.Utilities.AddPortsToWire(existWire, ports);
        }

        private void CreateWireForPorts(params PortPointer[] ports)
        {
            int canConnect = 0;
            PortPointer zero = ports[0];
            for (int i = 1; i < ports.Length; i++)
            {
                if (zero.Port.CanConnect(ports[i].Port)) canConnect++;
            }
                
            if(canConnect == 0) return;
                
            Wire newWire = zero.Port.CreateWire();
            Graph.Wires.Utilities.AddPortsToWire(newWire, ports);
            AddWire(newWire);
        }

        private List<PortPointer> GetAllPorts()
        {
            List<PortPointer> result = new List<PortPointer>();
            foreach (IGraphNode structureBlock in nodes)
            {
                GraphUtilities.GetAllPorts(structureBlock, ref result);
            }

            return result;
        }
    }
}