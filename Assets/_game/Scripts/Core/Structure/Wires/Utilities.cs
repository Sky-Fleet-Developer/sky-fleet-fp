using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.Structure.Wires
{
    public static class Utilities
    {
        private static List<System.Type> storageItemTypes;

        public static IEnumerable<string> GetStorageItemNames()
        {
            return GetStorageItemTypes().Select(x => x.Name);
        }

        public static IEnumerable<System.Type> GetStorageItemTypes()
        {
            if (storageItemTypes == null)
            {
                storageItemTypes = new List<System.Type>();
                Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    foreach (System.Type type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(StorageItem)))
                        {
                            storageItemTypes.Add(type);
                        }
                    }
                }
                storageItemTypes.Add(typeof(StorageItem));
            }

            return storageItemTypes;
        }
        
        public static void GetPortsDescriptions(IBlock block, ref List<IPortsContainer> container)
        {
            if (block is IMultiplePorts multiplePorts)
            {
                GetPortsFromSpecialBlock(block.transform.name, multiplePorts, ref container);
            }
            else
            {
                GetPortsFromBlock(block, ref container);
            }
        }

        private static void GetPortsFromBlock(IBlock block, ref List<IPortsContainer> container)
        {
            FieldInfo[] fields = Factory.GetPortsInfo(block);
            List<PortPointer> pointers = new List<PortPointer>();
            Factory.GetAllPorts(block, ref pointers);
            List<IPortsContainer> infos = new List<IPortsContainer>(fields.Length);
            
            for (var i = 0; i < pointers.Count; i++)
            {
                string portName = GetNameOf(pointers[i].Port);
                infos.Add(new PortInfo(pointers[i], $"{fields[i].Name}: {portName}"));
            }
            
            container.Add(new PortsGroupContainer(block.transform.name, infos));
        }

        private static void GetPortsFromSpecialBlock(string blockName, IMultiplePorts block, ref List<IPortsContainer> container)
        {
            var multiplePortsFields = Factory.GetMultiplePortsFields(block);

            List<IPortsContainer> groups = new List<IPortsContainer>(multiplePortsFields.Length);

            foreach (FieldInfo field in multiplePortsFields)
            {
                if (field.GetValue(block) is IList value)
                {
                    List<IPortsContainer> infos = new List<IPortsContainer>(value.Count);

                    foreach (IPortUser portUser in value)
                    {
                        string description = portUser.GetPortDescription();
                        var port = portUser.GetPort();
                        
                        infos.Add(new PortInfo(new PortPointer(block, port), description));
                    }
                    groups.Add(new PortsGroupContainer(field.Name + ":", infos));
                }
            }
            container.Add(new PortsGroupContainer(blockName, groups));
        }

        private static string GetNameOf(Port port)
        {
            if (port is Port<float> f)
            {
                return f.ValueType.ToString();
            }if (port is PowerPort)
            {
                return "Power";
            }if (port is StoragePort stp)
            {
                return stp.serializedType == "Null" ? "Storage item" : stp.serializedTypeShort;
            }
            return string.Empty;
        }
        
        public static void CreateWireForPorts(IWiresMaster master, params PortPointer[] ports)
        {
            int canConnect = 0;
            PortPointer zero = ports[0];
            for (int i = 1; i < ports.Length; i++)
            {
                if (zero.Port.CanConnect(ports[i].Port)) canConnect++;
            }
                
            if(canConnect == 0) return;
                
            Wire newWire = zero.Port.CreateWire();
            AddPortsToWire(newWire, ports);
            master.AddWire(newWire);
        }

        public static void AddPortsToWire(Wire wire, params PortPointer[] ports)
        {
            PortPointer zero = ports[0];

            zero.Port.SetWire(wire);
            wire.ports.Add(zero);

            for (int i = 1; i < ports.Length; i++)
            {
                if(wire.ports.Contains(ports[i]) || ports[i].CanConnect(wire) == false) continue;
                ports[i].Port.SetWire(wire);
                wire.ports.Add(ports[i]);
            }
        }
    }
    
    public class PortInfo : IPortsContainer
    {
        private PortPointer port;
        private string description;
        public bool HasNestedValues => false;

        public PortInfo(PortPointer port, string description)
        {
            this.port = port;
            this.description = description;
        }

        public List<IPortsContainer> GetNestedValues() => null;

        public string GetDescription() => description;

        public Color GetColor() => PortsColorsData.Instance.GetPortColor(port);
        public PortPointer GetPort() => port;
    }
        
    public class PortsGroupContainer : IPortsContainer
    {
        private List<IPortsContainer> items;
        private string blockName;
        public bool HasNestedValues => true;
        public PortsGroupContainer(string blockName, List<IPortsContainer> items)
        {
            this.items = items;
            this.blockName = blockName;
        }

        public List<IPortsContainer> GetNestedValues() => items;

        public string GetDescription() => blockName;

        public Color GetColor() => Color.white;
        public PortPointer GetPort() => default;
    }
        
    public interface IPortsContainer
    {
        bool HasNestedValues { get; }
        List<IPortsContainer> GetNestedValues();
        string GetDescription();
        Color GetColor();
        PortPointer GetPort();
    }
}
