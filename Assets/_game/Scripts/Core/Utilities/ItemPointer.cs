using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPointer : MonoBehaviour
{
    [SerializeField] private Component[] _pointers;

    [NonSerialized] public Dictionary<string, Component> pointers;

    public void Awake()
    {
        pointers = new Dictionary<string, Component>();
        foreach (var hit in _pointers)
        {
            pointers.Add(hit.name.Replace("(Clone)", "") + hit.GetType().Name, hit);
        }
    }

    public T GetPointer<T>(string name) where T : Component
    {
        if(pointers.ContainsKey(name + typeof(T).Name) == false)
        {
            Debug.LogError("Has no pointer with name " + name + " in item " + gameObject.name);
            return null;
        }
        return pointers[name + typeof(T).Name] as T;
    }
}
