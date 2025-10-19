using System;
using System.Collections.Generic;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Serialization;

namespace Core.Graph
{
    public interface IGraph
    {
        void SetConfiguration(GraphConfiguration value);
        void Init();
        void UpdateGraph();
        PortPointer GetPort(string id);
        IEnumerable<IGraphNode> Nodes { get; }
        IEnumerable<Wire> Wires { get; }
        public void AddNode(IGraphNode node);

        public void RemoveNode(IGraphNode node);
        public event Action<Wire> OnWireAdded;
        public event Action<Wire> OnWireRemoved;
    }

    public interface IGraphNode
    {
        void InitNode(IGraph graph);
        string NodeId { get; }
    }
}