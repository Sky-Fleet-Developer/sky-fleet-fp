using System.Threading.Tasks;
using UnityEngine;

namespace Core.Structure.Serialization
{
    public abstract class Configuration
    {
        public abstract Task TryApply<T>(T target) where T : class;
    }
    public abstract class Configuration<T> : Configuration where T : class
    {
        public Configuration(){}
        public Configuration(T value)
        {
        }
        public abstract Task Apply(T target);
        public T GetTFromGameObject(GameObject root) => root.GetComponent<T>();
        public override Task TryApply<T1>(T1 target)
        {
            if (target is T converted)
            {
                return Apply(converted);
            }
            return Task.CompletedTask;
        }
    }
}
