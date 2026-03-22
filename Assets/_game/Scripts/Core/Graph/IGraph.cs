using System;
using System.Collections.Generic;
using System.Linq;
using Core.Graph.Wires;
using Core.Items;
using Core.Misc;
using Core.Structure;
using Core.Structure.Serialization;

namespace Core.Graph
{
    public interface IGraph
    {
        void Init(IEnumerable<WireConfiguration> wires, bool autoConnectPowerWires);
        void UpdateGraph();
        PortPointer GetPort(string id);
        IEnumerable<IGraphNode> Nodes { get; }
        IEnumerable<Wire> Wires { get; }
        public void AddNode(IGraphNode node);

        public void RemoveNode(IGraphNode node);
        public event Action<Wire> OnWireAdded;
        public event Action<Wire> OnWireRemoved;
    }

    public static class GraphExtensions
    {
        public static bool InitGraphFromProperties(this IGraph graph, IPropertiesContainer propertiesContainer)
        {
            if (propertiesContainer.TryGetProperty(Property.WiresPropertyName, out var wiresProp) && propertiesContainer.TryGetProperty(Property.AutoConnectPowerWirePropertyName, out var acpwProp))
            {
                graph.Init(wiresProp.values.Select(x => x.GetObjectValue<WireConfiguration>()), acpwProp.values[0].intValue == 1);
                return true;
            }
            return false;
        }
    }

    public interface IGraphNode
    {
        void InitNode(IGraph graph);
        string NodeId { get; }
    }
}