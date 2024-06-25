using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Graph;
using Core.Graph.Wires;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Serialization
{
    [System.Serializable]
    public class GraphConfiguration : Configuration<IGraph>
    {
        public List<WireConfiguration> wires = new List<WireConfiguration>();
        [Button]
        public void CopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(wires);
        }
        [Button]
        public void PasteFromClipboard()
        {
            wires = JsonConvert.DeserializeObject<List<WireConfiguration>>(GUIUtility.systemCopyBuffer);
        }
        
        public GraphConfiguration(IGraph value) : base(value)
        {
            foreach (Wire wire in value.Wires)
            {
                wires.Add(new WireConfiguration(wire.ports.Select(x => x.Id).ToList()));
            }
        }
        
        public override Task Apply(IGraph graph)
        {
            graph.InitGraph();
            foreach (WireConfiguration wire in wires)
            {
                if (wire.ports.Count == 0)
                {
                    continue;
                }
                PortPointer[] portsToConnect = new PortPointer[wire.ports.Count];

                for (var i = 0; i < wire.ports.Count; i++)
                {
                    portsToConnect[i] = graph.GetPort(wire.ports[i]);
                }

                graph.ConnectPorts(portsToConnect);
            }
            return Task.CompletedTask;
        }


    }
    
    [System.Serializable]
    public class WireConfiguration
    {
        public List<string> ports;

        public WireConfiguration(List<string> ports)
        {
            this.ports = ports;
        }
    }
}
