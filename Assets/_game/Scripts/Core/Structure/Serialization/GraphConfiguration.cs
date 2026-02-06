using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.Graph;
using Core.Graph.Wires;
using Core.Items;
using Core.Misc;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Serialization
{
    [System.Serializable]
    public class GraphConfiguration : Configuration<IStructure>
    {
        public List<WireConfiguration> wires = new List<WireConfiguration>();
        public bool autoConnectPowerWires;
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
        public GraphConfiguration() {}

        public GraphConfiguration(ItemInstance source)
        {
            if (!source.TryGetProperty(Property.WiresPropertyName, out var property))
            {
                wires = new List<WireConfiguration>();
                return;
            }
            wires = new List<WireConfiguration>(property.values.Length);
            for (int i = 0; i < property.values.Length; i++)
            {
                wires.Add(property.values[i].GetObjectValue<WireConfiguration>());
            }
            
            if (!source.TryGetProperty(Property.AutoConnectPowerWirePropertyName, out property))
            {
                autoConnectPowerWires = false;
            }
            else
            {
                autoConnectPowerWires = property.values[0].intValue == 1;
            }
        }
        
        public GraphConfiguration(IStructure value) : base(value)
        {
            foreach (Wire wire in value.Graph.Wires)
            {
                wires.Add(new WireConfiguration(wire.ports.Select(x => x.Id).ToList()));
            }
        }

        public void RecordToItem(ItemInstance item)
        {
            var property = item.EnsureProperty(Property.WiresPropertyName);
            property.values = wires.Select(x => new PropertyValue(x)).ToArray();
        }
        
        public override Task Apply(IStructure structure)
        {
            structure.Graph.SetConfiguration(this);
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
        
        public class Serializer : ISerializer<WireConfiguration>
        {
            public void Serialize(WireConfiguration obj, Stream stream)
            {
                stream.WriteByte((byte)obj.ports.Count);
                foreach (string port in obj.ports)
                {
                    stream.WriteString(port);
                }
            }

            public WireConfiguration Deserialize(Stream stream)
            {
                var value = new WireConfiguration(new List<string>());
                Populate(stream, ref value);
                return value;
            }

            public void Populate(Stream stream, ref WireConfiguration obj)
            {
                obj.ports = new List<string>(stream.ReadByte());

                for (int i = 0; i < obj.ports.Capacity; i++)
                {
                    obj.ports.Add(stream.ReadString());
                }
            }
        }
    }
}
