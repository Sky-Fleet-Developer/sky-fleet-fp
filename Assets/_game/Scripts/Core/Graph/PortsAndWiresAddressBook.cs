using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure.Serialization;

namespace Core.Graph
{
    public class PortsAndWiresAddressBook
    {
        private readonly Dictionary<string, WireConfiguration> _wireConfigs;
        private Dictionary<WireConfiguration, Wire> _wireByConfig;
        private Dictionary<string, Wire> _wireByPort;
        private Dictionary<string, List<PortPointer>> _portsByNode;
        private Dictionary<string, PortPointer> _portsById;

        public PortsAndWiresAddressBook(GraphConfiguration configuration)
        {
            _wireConfigs = new();
            _wireByPort = new();
            _portsByNode = new();
            _wireByConfig = new();
            _portsById = new();
            foreach (var wire in configuration.wires)
            {
                foreach (var port in wire.ports) _wireConfigs.Add(port, wire);
            }
        }

        public bool TryGetWire(string portId, out WireConfiguration config, out Wire existWire)
        {
            if (_wireConfigs.TryGetValue(portId, out config))
            {
                _wireByConfig.TryGetValue(config, out existWire);
                return true;
            }

            existWire = null;
            return false;
        }

        public bool TryGetWire(string portId, out Wire wire) => _wireByPort.TryGetValue(portId, out wire);
        public void AddWireWithPorts(WireConfiguration configuration, Wire wire)
        {
            foreach (var portPointer in wire.ports)
            {
                _wireByPort.Add(portPointer.Id, wire);
            }
            _wireByConfig[configuration] = wire;
        }
        public void SetPortWire(string portId, Wire wire) => _wireByPort[portId] = wire;
        public void RemovePort(string portId) => _wireByPort.Remove(portId);
        public void SetNodePorts(string nodeId, List<PortPointer> ports)
        {
            _portsByNode[nodeId] = ports;
            foreach (var portPointer in ports)
            {
                _portsById.Add(portPointer.Id, portPointer);
            }
        }

        public bool TryGetPorts(string nodeId, out List<PortPointer> ports) => _portsByNode.TryGetValue(nodeId, out ports);
        public void RemoveNode(string nodeId)
        {
            foreach (var portPointer in _portsByNode[nodeId])
            {
                _portsById.Remove(portPointer.Id);
            }
            _portsByNode.Remove(nodeId);
        }

        public PortPointer GetPort(string id)
        {
            return _portsById[id];
        }
    }
}