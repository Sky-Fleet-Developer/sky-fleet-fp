using System.Collections;
using System.Collections.Generic;

namespace Core.World
{
    /// <summary>
    /// Data attached to a world entity. Every component class must declare a nested <c>Serializer</c>
    /// (see <see cref="Core.ContentSerializer.ISerializer{T}"/>), because components are saved with the entity.
    /// </summary>
    public interface IEntityComponent
    {
    }

    public interface IEntityComponents : IEnumerable<IEntityComponent>
    {
        int Count { get; }
        bool TryGet<T>(out T component) where T : class, IEntityComponent;
        /// <returns>The component or null.</returns>
        T Get<T>() where T : class, IEntityComponent;
        /// <summary>Adds the component or replaces the existing one of the same type.</summary>
        void Set(IEntityComponent component);
        bool Remove<T>() where T : class, IEntityComponent;
    }

    public class EntityComponents : IEntityComponents
    {
        private List<IEntityComponent> _items;

        public int Count => _items?.Count ?? 0;

        public bool TryGet<T>(out T component) where T : class, IEntityComponent
        {
            if (_items != null)
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    if (_items[i] is T found)
                    {
                        component = found;
                        return true;
                    }
                }
            }

            component = null;
            return false;
        }

        public T Get<T>() where T : class, IEntityComponent
        {
            TryGet<T>(out var component);
            return component;
        }

        public void Set(IEntityComponent component)
        {
            _items ??= new List<IEntityComponent>(2);
            var type = component.GetType();
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].GetType() != type) continue;
                _items[i] = component;
                return;
            }

            _items.Add(component);
        }

        public bool Remove<T>() where T : class, IEntityComponent
        {
            if (_items == null) return false;
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i] is not T) continue;
                _items.RemoveAt(i);
                return true;
            }

            return false;
        }

        public IEnumerator<IEntityComponent> GetEnumerator()
        {
            if (_items == null) yield break;
            for (var i = 0; i < _items.Count; i++)
            {
                yield return _items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
