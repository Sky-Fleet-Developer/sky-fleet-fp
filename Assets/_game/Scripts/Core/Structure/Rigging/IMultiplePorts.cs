using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Structure.Wires;
using Sirenix.Utilities;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IMultiplePorts : IBlock
    {
        IEnumerable<PortPointer> GetPorts();
    }
    
    public static partial class Utilities
    {
        public static void GetPortsFromSpecialBlock(string blockName, IMultiplePorts block,
            ref List<IPortsContainer> container)
        {
            var multiplePortsFields = GetMultiplePortsFields(block);

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
        
        
        public static Dictionary<Type, FieldInfo[]> MultiplePorts;

        public static FieldInfo[] GetMultiplePortsFields(IMultiplePorts block)
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
                if (field.FieldType.InheritsFrom(type) && field.FieldType.GetGenericArguments().FirstOrDefault(x => x.InheritsFrom(elementType)) != null)
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
    }
}
