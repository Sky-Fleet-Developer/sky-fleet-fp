using System.Collections.Generic;
using Core.Structure.Wires;

namespace Core.Structure
{
    public interface IWiresMaster
    {
        IEnumerable<Wire> Wires { get; }
        void AddWire(Wire wire);
        PortPointer GetPort(string id);
        void ConnectPorts(params PortPointer[] ports);
    }
}