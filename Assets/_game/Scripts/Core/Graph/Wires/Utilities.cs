using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.Graph.Wires
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
        
        public static void GetPortsDescriptions(IGraphNode node, ref List<IPortsContainer> container)
        {
            List<IPortsContainer> subContainers = new List<IPortsContainer>();
            container.Add(new PortsGroupContainer(node.NodeId, subContainers));
            //subContainers.AddRange(Graph.GraphUtilities.GetPortsFromSpecialBlock(node.NodeId, node));
            subContainers.AddRange(GetPortsFromBlock(node));
        }

        private static IEnumerable<IPortsContainer> GetPortsFromBlock(IGraphNode node)
        {
            List<PortPointer> pointers = new List<PortPointer>();
            GraphUtilities.GetPorts(node, ref pointers); 
            IEnumerable<IGrouping<string, PortPointer>> group = pointers.GroupBy(x => x.GetGroup());
            foreach (IGrouping<string, PortPointer> portPointers in group)
            {
                PortPointer[] array = portPointers.ToArray();
                if (array.Length == 1)
                {
                    string portName = GetNameOf(array[0].Port);
                    yield return new PortInfo(array[0], $"{portPointers.Key}: {portName}");
                }
                else
                {
                    List<IPortsContainer> infos = new List<IPortsContainer>(array.Length);
                    foreach (var portPointer in array)
                    {
                        infos.Add(new PortInfo(portPointer, $"{portPointer.Id}: {GetNameOf(portPointer.Port)}"));   
                    }
                    yield return new PortsGroupContainer(portPointers.Key + ":", infos);
                }
            }
        }

        private static string GetNameOf(Port port)
        {
            if (port is Port<float> f)
            {
                return f.ValueType.ToString();
            }
            if (port is Port<Vector2> f2)
            {
                return $"{f2.ValueType.ToString()}(vector 2)";
            }
            if (port is Port<Vector3> f3)
            {
                return $"{f3.ValueType.ToString()}(vector 3)";
            }
            if (port is ActionPort)
            {
                return "Action";
            }
            if (port is PowerPort)
            {
                return "Power";
            }
            if (port is AimingInterfacePort)
            {
                return "Aiming interface";
            }
            if (port is StoragePort stp)
            {
                return stp.serializedType == "Null" ? "Storage item" : stp.serializedTypeShort;
            }
            return string.Empty;
        }
        
        public static void CreateWireForPorts(IGraph master, params PortPointer[] ports)
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
            PortPointer zero = ports.First(x => x.Port != null);

            zero.Port.SetWire(wire);
            wire.ports.Add(zero);

            for (int i = 0; i < ports.Length; i++)
            {
                if(ports[i].Equals(zero) || ports[i].Port == null) continue;
                
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
        private string nodeId;
        public bool HasNestedValues => true;
        public PortsGroupContainer(string nodeId, List<IPortsContainer> items)
        {
            this.items = items;
            this.nodeId = nodeId;
        }

        public List<IPortsContainer> GetNestedValues() => items;

        public string GetDescription() => nodeId;

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
