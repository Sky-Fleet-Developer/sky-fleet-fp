using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Graph.Wires;
using Core.Structure;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Core.Graph
{
    public class StructureGraphBehaviour : MonoBehaviour, IGraph
    {
        private IStructure structure;
        [ShowInInspector] private List<Wire> wires = new List<Wire>();
        private Dictionary<string, PortPointer> portsCache;
        private List<PortPointer> portsPointersCache;
        public List<IGraphNode> Nodes { get; private set; }

        
        private void Awake()
        {
            structure ??= GetComponent<IStructure>();
            structure.OnInitComplete.Subscribe(InitGraph);
        }

        public void InitGraph()
        {
            structure ??= GetComponent<IStructure>();
            Nodes = new List<IGraphNode>();
            portsCache = new Dictionary<string, PortPointer>();
            if (structure == null)
            {
                Debug.LogError($"{transform.name} has no IStructure component but try to init structure graph!");
                return;
            }

            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                if (structure.Blocks[i] is IGraphNode node)
                {
                    Nodes.Add(node);
                    node.InitNode(this);
                }
            }
        }

        public void AddWire(Wire wire)
        {
            wires.Add(wire);
        }

        public PortPointer GetPort(string id)
        {
            if (portsCache.TryGetValue(id, out PortPointer port)) return port;

            portsPointersCache ??= GetAllPorts();

            port = portsPointersCache.FirstOrDefault(x => x.Id.Equals(id));
            portsCache.Add(id, port);
            return port;
        }

        public void ConnectPorts(params PortPointer[] ports)
        {
            Wire existWire = null;

            foreach (PortPointer port in ports)
            {
                existWire = port.Port.GetWire();
                if (existWire != null) break;
            }

            if (existWire == null) Graph.Wires.Utilities.CreateWireForPorts(this, ports);
            else Graph.Wires.Utilities.AddPortsToWire(existWire, ports);
        }


        private List<PortPointer> GetAllPorts()
        {
            List<PortPointer> result = new List<PortPointer>();
            foreach (IGraphNode structureBlock in Nodes)
            {
                GraphUtilities.GetAllPorts(structureBlock, ref result);
            }

            return result;
        }
    }
}