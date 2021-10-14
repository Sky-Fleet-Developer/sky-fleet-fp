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

        public (GameObject, Component) Find(GameObject prefab, bool remuve = false)
        {
            DynamicPool inst = Instance;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Item1 == prefab)
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
            T t = Get<T>(source.gameObject);
            t.transform.SetParent(parent);
            t.transform.localPosition = Vector3.zero;
            t.transform.localRotation =Quaternion.identity;
            t.transform.localScale = Vector3.one;
            return t;
        }

        public T Get<T>(T source) where T : Component
        {
            return Get<T>(source.gameObject);
        }

        public T Get<T>(GameObject prefab) where T : Component
        {
            (GameObject, Component) target = Find(prefab, true);
            if (target.Item1 == null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name + $"({all.Count(x => x.Item1 == prefab)})";
                target = (prefab, instance.GetComponent<T>());
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
            for (int i = 0; i < free.Count; i++)
            {
                if (free[i].Item2 == component)
                {
                    if (!component) return;
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