using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Graph.Wires;
using Sirenix.Utilities;
using UnityEngine;

namespace Core.Graph
{
    public static class GraphUtilities
    {
        public static void GetPortsFromSpecialBlock(string nodeId, IMultiplePortsNode node,
            ref List<IPortsContainer> container)
        {
            var multiplePortsFields = GetMultiplePortsFields(node);

            List<IPortsContainer> groups = new List<IPortsContainer>(multiplePortsFields.Length);

            foreach (FieldInfo field in multiplePortsFields)
            {
                if (field.GetValue(node) is IList value)
                {
                    List<IPortsContainer> infos = new List<IPortsContainer>(value.Count);

                    foreach (IPortUser portUser in value)
                    {
                        string description = portUser.GetPortDescription();
                        var port = portUser.GetPort();

                        infos.Add(new PortInfo(new PortPointer(node, port), description));
                    }

                    groups.Add(new PortsGroupContainer(field.Name + ":", infos));
                }
            }

            container.Add(new PortsGroupContainer(nodeId, groups));
        }
        
        
        public static Dictionary<Type, FieldInfo[]> MultiplePorts;

        public static FieldInfo[] GetMultiplePortsFields(IMultiplePortsNode block)
        {
            Type blockType = block.GetType();
            if (MultiplePorts == null) MultiplePorts = new Dictionary<Type, FieldInfo[]>();
            
            if (MultiplePorts.TryGetValue(blockType, out FieldInfo[] infos)) return infos;
                
            List<FieldInfo> fields = new List<FieldInfo>();

            Type type = typeof(IList);
            Type elementType = typeof(IPortUser);

            string log = $"Ports for type {blockType.Name}:\n";

            FieldInfo[] allFields = blockType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            
            foreach (FieldInfo field in allFields)
            {
                if (field.FieldType.InheritsFrom(type) && field.FieldType.GetGenericArguments().FirstOrDefault(x => TypeExtensions.InheritsFrom(x, elementType)) != null)
                {
                    fields.Add(field);
                    log += $"{field.Name},";
                }
            }

            Debug.Log(log);

            infos = fields.ToArray();
            
            MultiplePorts.Add(blockType, infos);

            return infos;
        }
        
        private static Dictionary<Type, FieldInfo[]> _blocksPorts;
        public static FieldInfo[] GetPortsInfo(IGraphNode node)
        {
            Type blockType = node.GetType();

            if (_blocksPorts == null) _blocksPorts = new Dictionary<Type, FieldInfo[]>();
            if (_blocksPorts.TryGetValue(blockType, out FieldInfo[] infos)) return infos;

            List<FieldInfo> fields = new List<FieldInfo>();

            Type type = typeof(Port);

            //string log = $"Ports for type {blockType.Name}:\n";

            foreach (FieldInfo field in blockType.GetFields())
            {
                if (field.FieldType == type || field.FieldType.InheritsFrom(type))
                {
                    fields.Add(field);
                    //log += $"{field.Name},";
                }
            }

            //Debug.Log(log);

            infos = fields.ToArray();

            _blocksPorts.Add(blockType, infos);

            return infos;
        }
        
        public static void GetAllPorts(IGraphNode node, ref List<PortPointer> result)
        {
            GetPorts(node, ref result);
            if (node is IMultiplePortsNode multiplePortsNode)
            {
                GetMultiplePorts(multiplePortsNode, ref result);
            }
        }

        public static void GetPorts(IGraphNode node, ref List<PortPointer> result)
        {
            FieldInfo[] properties = GraphUtilities.GetPortsInfo(node);
            foreach (FieldInfo property in properties)
            {
                result.Add(new PortPointer(node, property.GetValue(node) as Port));
            }
        }

        public static void GetMultiplePorts(IMultiplePortsNode multiplePortsNode, ref List<PortPointer> result)
        {
            IEnumerable<PortPointer> specialPorts = multiplePortsNode.GetPorts();
            result.AddRange(specialPorts);
        }
    }
}
