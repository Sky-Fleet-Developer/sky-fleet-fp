using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Utilities
{
    public class DynamicPool : Singleton<DynamicPool>
    {
        private List<(GameObject, Component)> pool;
        private List<(GameObject, Component)> free;
        private List<(GameObject, Component)> all;

        protected override void Setup()
        {
            free = new List<(GameObject, Component)>();
            pool = new List<(GameObject, Component)>();
            all = new List<(GameObject, Component)>();
        }

        public (GameObject, Component) Find(Component prefab, bool remuve = false)
        {
            DynamicPool inst = Instance;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Item1 == prefab.gameObject)
                {
                    if (remuve)
                    {
                        (GameObject, Component) value = pool[i];
                        pool.RemoveRange(i, 1);
                        return value;
                    }
                    return pool[i];
                }
            }
            return (null, null);
        }

        public T Get<T>(T source, Transform parent) where T : Component
        {
            T t = Get<T>(source);
            var tr = t.transform;
            tr.SetParent(parent);
            tr.localPosition = Vector3.zero;
            tr.localRotation =Quaternion.identity;
            tr.localScale = Vector3.one;
            if (tr is RectTransform rtr)
            {
                rtr.anchoredPosition = Vector2.zero;
            }
            return t;
        }

        public T Get<T>(T prefab) where T : Component
        {
            (GameObject, Component) target = Find(prefab, true);
            if (target.Item1 == null)
            {
                T instance = Instantiate(prefab);
                instance.name = prefab.name + $"({all.Count(x => x.Item1 == prefab)})";
                target = (prefab.gameObject, instance);
                all.Add(target);
            }
            free.Add(target);
            target.Item2.gameObject.SetActive(true);
            target.Item2.transform.SetParent(null);
            return target.Item2 as T;
        }

        public void Return(Component component, float delay)
        {
            this.Wait(delay, () => Return(component));
        }

        public void Return(Component component)
        {
            if (!component) return;

            for (int i = 0; i < free.Count; i++)
            {
                if (free[i].Item2 == component)
                {
                    component.transform.SetParent(Instance.transform);
                    component.gameObject.SetActive(false);
                    pool.Add(free[i]);
                    free.RemoveRange(i, 1);
                    return;
                }
            }

            if (Application.isPlaying)
            {
                Destroy(component.gameObject);
            }
            else
            {
                DestroyImmediate(component.gameObject);
            }

            Debug.LogError($"Has no component {component} in pool!");
        }
    }
}