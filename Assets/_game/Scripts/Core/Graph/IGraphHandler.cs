using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure.Serialization;

namespace Core.Graph
{
    public interface IGraph : IGraphHandler
    {
        void SetConfiguration(GraphConfiguration value);
        void InitGraph(bool force = false);
    }
    public interface IGraphHandler
    {
        void AddNewWire(Wire wire);
        PortPointer GetPort(string id);
        IEnumerable<IGraphNode> Nodes { get; }
        IEnumerable<Wire> Wires { get; }
        IEnumerable<PortPointer> Ports { get; }
    }

    public interface IGraphNode
    {
        void InitNode(IGraphHandler graph);
        string NodeId { get; }
    }
}