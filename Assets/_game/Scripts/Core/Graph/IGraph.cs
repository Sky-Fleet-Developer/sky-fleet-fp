using System.Collections.Generic;
using Core.Graph.Wires;

namespace Core.Graph
{
    public interface IGraph
    {
        void InitGraph();
        void AddWire(Wire wire);
        PortPointer GetPort(string id);
        void ConnectPorts(params PortPointer[] ports);
        IEnumerable<IGraphNode> Nodes { get; }
        IEnumerable<Wire> Wires { get; }
    }

    public interface IGraphNode
    {
        void InitNode(IGraph graph);
        string NodeId { get; }
    }
}