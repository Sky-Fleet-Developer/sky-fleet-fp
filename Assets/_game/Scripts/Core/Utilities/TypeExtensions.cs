using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core.Utilities
{
    public static class TypeExtensions
    {
        //public static bool TypesDirty = false;
        private static readonly Dictionary<string, Type> _typesCache;

        private static List<string> _assembliesQueueReference = new()
        {
            "Core",
            "Runtime",
            "Assembly-CSharp"
        };
        private static List<Assembly> _assembliesQueue = new();

        static TypeExtensions()
        {
            //if (TypesCache != null)
            //{
            //    return;
            //}
            _typesCache = new Dictionary<string, Type>();
            
            _assembliesQueue = new List<Assembly>( AppDomain.CurrentDomain.GetAssemblies());
            _assembliesQueue.Sort((a, b) =>
            {
                int aIndex = _assembliesQueueReference.IndexOf(a.GetName().Name);
                int bIndex = _assembliesQueueReference.IndexOf(b.GetName().Name);
                
                if (aIndex == -1 && bIndex == -1)
                {
                    return 0;
                }

                if (aIndex == -1)
                {
                    return 1;
                }

                if (bIndex == -1)
                {
                    return -1;
                }
                
                return aIndex.CompareTo(bIndex);
            });
            
            Assert.AreEqual(_assembliesQueue[0].GetName().Name, "Core", "Something went wrong! Check the ordering of assemblies");
            
            //foreach (var assembly in _assembliesQueue)
            //{
            //    Debug.LogError(assembly.GetName().Name);
            //}
        }
        
        public static Type GetTypeByName(string name)
        {
            if (!_typesCache.TryGetValue(name, out Type result))
            {
                for (var i = 0; i < _assembliesQueue.Count; i++)
                {
                    foreach (Type type in _assembliesQueue[i].GetTypes())
                    {
                        if (type.FullName != null)
                        {
                            _typesCache[type.FullName] = type;
                        }
                    }
                    _assembliesQueue.RemoveAt(i--);
                    if (_typesCache.TryGetValue(name, out result))
                    {
                        return result;
                    }
                }
            }
            return result;
        }
    }
}
