using System;

namespace Core.Graph.Wires
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PortGroupAttribute : Attribute
    {
        public readonly string Group;

        public PortGroupAttribute(string group)
        {
            Group = group;
        }
    }
}