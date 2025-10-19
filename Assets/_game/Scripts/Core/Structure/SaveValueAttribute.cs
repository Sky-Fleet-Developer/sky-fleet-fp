using System;

namespace Core.Structure
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveValueAttribute : Attribute { }
}