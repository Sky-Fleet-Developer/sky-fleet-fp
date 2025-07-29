using System;
using System.Collections.Generic;
using System.Linq;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Graph
{
    public class StructureGraphBehaviour : MonoBehaviour, IGraph
    {
        private IStructure structure;
        private Dictionary<string, PortPointer> portsCache;
        private List<PortPointer> portsPointers;
        private List<IGraphNode> nodes = new List<IGraphNode>();
        [ShowInInspector] private List<Wire> wires = new List<Wire>();

        public IEnumerable<IGraphNode> Nodes => nodes;
        public IEnumerable<Wire> Wires => wires;
        public IEnumerable<PortPointer> Ports => portsPointers;
        private bool _isInitialized = false;
        private GraphConfiguration _configuration;

        private void Awake()
        {
            structure ??= GetComponent<IStructure>();
            structure.OnInitComplete.Subscribe(() => InitGraph(true), -1);
        }

        public void SetConfiguration(GraphConfiguration value)
        {
            _configuration = value;
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

            if (force || portsPointers == null)
            {
                portsPointers = GetAllPorts();
            }
            
            foreach (WireConfiguration wire in _configuration.wires)
            {
                if (wire.ports.Count == 0)
                {
                    continue;
                }
                PortPointer[] portsToConnect = new PortPointer[wire.ports.Count];

                int notNullCounter = 0;
                for (var i = 0; i < wire.ports.Count; i++)
                {
                    portsToConnect[i] = GetPort(wire.ports[i]);
                    if (!portsToConnect[i].IsNull())
                    {
                        notNullCounter++;
                    }
                }

                if (notNullCounter > 1)
                {
                    ConnectPorts(portsToConnect);
                }
            }

            if (_configuration.autoConnectPowerWires)
            {
                PortPointer[] powerPorts = Ports.Where(x => x.Port is PowerPort).ToArray();
                if (powerPorts.Length > 0)
                {
                    ConnectPorts(powerPorts);
                }
            }

            _isInitialized = true;
        }

        public void AddNewWire(Wire wire)
        {
            wires.Add(wire);
        }

        public PortPointer GetPort(string id)
        {
            if (portsCache.TryGetValue(id, out PortPointer port)) return port;
            
            port = portsPointers.FirstOrDefault(x => x.Id.Equals(id));

            if (!port.IsNull())
            {
                portsCache.Add(id, port);
            }
            return port;
        }
        
        private void ConnectPorts(params PortPointer[] ports)
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
            AddNewWire(newWire);
        }

        private List<PortPointer> GetAllPorts()
        {
            List<PortPointer> result = new List<PortPointer>();
            foreach (IGraphNode structureBlock in nodes)
            {
                GraphUtilities.GetPorts(structureBlock, ref result);
            }

            return result;
        }
    }
}