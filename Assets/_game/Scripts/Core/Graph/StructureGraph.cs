using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Graph
{
    public class StructureGraph : IGraph, IDisposable
    {
        private Dictionary<string, PortPointer> _portsCache;
        private List<PortPointer> _portsPointers;
        private List<IGraphNode> _nodes;
        private PowerPortProcessor _powerPortProcessor;
        [ShowInInspector] private List<Wire> _wires;
        public event Action<Wire> OnWireAdded;
        public event Action<Wire> OnWireRemoved;

        public IEnumerable<IGraphNode> Nodes => _nodes;
        public IEnumerable<Wire> Wires => _wires;
        public IEnumerable<PortPointer> Ports => _portsPointers;
        private bool _isInitialized = false;
        private GraphConfiguration _configuration;
        private PortsAndWiresAddressBook _addressBook;
        public void SetConfiguration(GraphConfiguration value)
        {
            _configuration = value;
            _addressBook = new PortsAndWiresAddressBook(_configuration);
        }

        public void Init()
        {
            if(_isInitialized) return;
            _powerPortProcessor = new PowerPortProcessor(this);
            _portsCache = new Dictionary<string, PortPointer>();
            _nodes = new List<IGraphNode>();
            _wires = new List<Wire>();
            /*

            portsPointers = GetAllPorts();
            
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
*/
            _isInitialized = true;
        }

        public void AddNode(IGraphNode node)
        {
            _nodes.Add(node);
            node.InitNode(this);
            List<PortPointer> ports = new List<PortPointer>();
            GraphUtilities.GetPorts(node, ref ports);
            _addressBook.SetNodePorts(node.NodeId, ports);
            foreach (var port in ports)
            {
                if (_addressBook.TryGetWire(port.Id, out WireConfiguration wireConfiguration, out Wire existWire))
                {
                    if (existWire != null)
                    {
                        if (existWire.CanConnect(port))
                        {
                            Core.Graph.Wires.Utilities.AddPortsToWire(existWire, port);
                            _addressBook.SetPortWire(port.Id, existWire);
                        }
                    }
                    else
                    {
                        var wire = Core.Graph.Wires.Utilities.CreateWireForPort(port);
                        _wires.Add(wire);
                        _addressBook.AddWireWithPorts(wireConfiguration, wire);
                        OnWireAdded?.Invoke(wire);
                    }
                }
            }
        }

        public void RemoveNode(IGraphNode node)
        {
            List<Wire> affectedWires = new List<Wire>();
            _nodes.Remove(node);
            if (_addressBook.TryGetPorts(node.NodeId, out List<PortPointer> ports))
            {
                foreach (var port in ports)
                {
                    if (_addressBook.TryGetWire(port.Id, out Wire wire))
                    {
                        wire.ports.Remove(port);
                        affectedWires.Add(wire);
                    }
                }

                _addressBook.RemoveNode(node.NodeId);
            }

            foreach (var affectedWire in affectedWires)
            {
                if (affectedWire.ports.Count == 0)
                {
                    RemoveWire(affectedWire);
                }
            }
        }

        public void UpdateGraph()
        {
            _powerPortProcessor.DistributionTick();
        }
        
        private void RemoveWire(Wire wire)
        {
            _wires.Remove(wire);
            foreach (var port in wire.ports)
            {
                Core.Graph.Wires.Utilities.RemovePortFromWire(wire, port);
                _addressBook.RemovePort(port.Id);
            }
            wire.Dispose();
            OnWireRemoved?.Invoke(wire);
        }

        public PortPointer GetPort(string id)
        {
            if (_portsCache.TryGetValue(id, out PortPointer port)) return port;
            
            port = _portsPointers.FirstOrDefault(x => x.Id.Equals(id));

            if (!port.IsNull())
            {
                _portsCache.Add(id, port);
            }
            return port;
        }
        
        /*private void ConnectPorts(params PortPointer[] ports)
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
        }*/

        /*private void CreateWireForPorts(params PortPointer[] ports)
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
        }*/

        /*private List<PortPointer> GetAllPorts()
        {
            List<PortPointer> result = new List<PortPointer>();
            foreach (IGraphNode structureBlock in _nodes)
            {
                GraphUtilities.GetPorts(structureBlock, ref result);
            }

            return result;
        }*/

        public void Dispose()
        {
            _powerPortProcessor.Dispose();
        }
    }
}