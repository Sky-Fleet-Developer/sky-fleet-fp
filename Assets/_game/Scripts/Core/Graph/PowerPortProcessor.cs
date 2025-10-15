using System;
using System.Collections.Generic;
using Core.Graph.Wires;

namespace Core.Graph
{
    public class PowerPortProcessor : IDisposable
    {
        private List<PowerWire> _wires = new List<PowerWire>();
        private StructureGraph _graph;

        public PowerPortProcessor(StructureGraph graph)
        {
            _graph = graph;
            graph.OnWireAdded += OnWireAdded;
        }

        private void OnWireAdded(Wire wire)
        {
            if (wire is PowerWire powerWire)
            {
                _wires.Add(powerWire);
            }
        }

        private void OnWireRemoved(Wire wire)
        { 
            if (wire is PowerWire powerWire)
            {
                _wires.Remove(powerWire);
            }
        }

        public void DistributionTick()
        {
            for (var i = 0; i < _wires.Count; i++)
            {
                _wires[i].DistributionTick();
            }
        }

        public void Dispose()
        {
            _graph.OnWireAdded -= OnWireAdded;
            _graph = null;
        }
    }
}