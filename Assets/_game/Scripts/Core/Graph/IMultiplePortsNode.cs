using System.Collections.Generic;
using Core.Graph.Wires;

namespace Core.Graph
{
    public interface IMultiplePortsNode : IGraphNode
    {
        IEnumerable<PortPointer> GetPorts();
    }
}