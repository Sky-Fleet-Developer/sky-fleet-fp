using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Utilities
{
    public class ItemPointer : MonoBehaviour
    {
        [SerializeField] private Component[] _pointers;

        [NonSerialized] public Dictionary<string, Component> pointers;

        protected virtual void Awake()
        {
            pointers = new Dictionary<string, Component>();
            foreach (Component hit in _pointers)
            {
                string key;
                if (hit.transform == transform) key = "this";
                else key = hit.name.Replace("(Clone)", "");
                pointers.Add(key + hit.GetType().Name, hit);
            }
        }

        public T GetPointer<T>(string name) where T : Component
        {
            if (pointers.ContainsKey(name + typeof(T).Name) == false)
            {
                Debug.LogError("Has no pointer with name " + name + " in item " + gameObject.name);
                return null;
            }

            return pointers[name + typeof(T).Name] as T;
        }
        
        public virtual void SetVisual(params object[] args)
        {
        }
    }
}