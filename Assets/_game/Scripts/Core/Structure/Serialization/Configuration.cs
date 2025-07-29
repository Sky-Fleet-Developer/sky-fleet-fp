using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Graph.Wires;
using UnityEngine;

namespace Core.Structure.Serialization
{
    public abstract class Configuration
    {
        public abstract Task TryApply(GameObject target);
    }
    public abstract class Configuration<T> : Configuration where T : class
    {
        public Configuration(){}
        public Configuration(T value)
        {
        }
        public abstract Task Apply(T target);
        public override Task TryApply(GameObject target)
        {
            if (target.TryGetComponent(out T component))
            {
                return Apply(component);
            }
            return Task.CompletedTask;
        }
    }
}
