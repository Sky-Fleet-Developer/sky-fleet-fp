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
        /*public static IEnumerable<IPortsContainer> GetPortsFromSpecialBlock(string nodeId, IGraphNode node)
        {
            var listPortFields = GetListPortFields(node);

            foreach (FieldInfo field in listPortFields)
            {
                var value = field.GetValue(node);

                if (value is IList list) 
                {
                    List<IPortsContainer> infos = new List<IPortsContainer>(list.Count);

                    foreach (IPortUser portUser in list)
                    {
                        string description = portUser.GetGroup();
                        var port = portUser.GetPort();

                        infos.Add(new PortInfo(new PortPointer(node, port, description), description));
                    }

                    yield return new PortsGroupContainer(field.Name + ":", infos);
                }
            }
        }*/
        
        
        public static Dictionary<Type, FieldInfo[]> MultiplePorts;

        public static FieldInfo[] GetNestedPortUserFields(IGraphNode block)
        {
            Type blockType = block.GetType();
            if (MultiplePorts == null) MultiplePorts = new Dictionary<Type, FieldInfo[]>();
            
            if (MultiplePorts.TryGetValue(blockType, out FieldInfo[] infos)) return infos;
                
            List<FieldInfo> fields = new List<FieldInfo>();

            Type type = typeof(IList);
            Type elementType = typeof(IPortUser);

            FieldInfo[] allFields = blockType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            
            foreach (FieldInfo field in allFields)
            {
                if (field.FieldType.InheritsFrom(type) && field.FieldType.GetGenericArguments().Any(x => x.InheritsFrom(elementType)))
                {
                    fields.Add(field);
                }
            }

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
            List<Type> typeTree = new List<Type>();
            var t = blockType;
            while (t != null && t.GetInterfaces().Contains(typeof(IGraphNode)))
            {
                typeTree.Add(t);
                t = t.BaseType;
            }
            Type type = typeof(Port);

            //string log = $"Ports for type {blockType.Name}:\n";
            for (var i = 0; i < typeTree.Count; i++)
            {
                foreach (FieldInfo field in typeTree[i].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.FieldType == type || field.FieldType.InheritsFrom(type))
                    {
                        fields.Add(field);
                        //log += $"{field.Name},";
                    }
                }
            }
            //Debug.Log(log);

            infos = fields.ToArray();

            _blocksPorts.Add(blockType, infos);

            return infos;
        }
        
        public static void GetPorts(IGraphNode node, List<PortPointer> result)
        {
            FieldInfo[] fields = GetPortsInfo(node); 
            foreach (FieldInfo field in fields)
            {
                string group = null;//field.Name;
                var groupAttribute = field.GetCustomAttribute<PortGroupAttribute>();
                if (groupAttribute != null)
                {
                    group = groupAttribute.Group;
                }
                result.Add(new PortPointer(node, field.GetValue(node) as Port, field.Name, group));
            }
            var listPortFields = GetNestedPortUserFields(node);
            foreach (var field in listPortFields)
            {
                if (field.GetValue(node) is IList list)
                {
                    foreach (IPortUser portUser in list)
                    {
                        string description = portUser.GetName();
                        var port = portUser.GetPort();

                        result.Add(new PortPointer(node, port, description, field.Name));
                    }
                }
            }
        }
    }
}
