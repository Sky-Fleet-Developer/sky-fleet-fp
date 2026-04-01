using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.Utilities
{
    public static class TypeExtensions
    {
        //public static bool TypesDirty = false;
        public static Dictionary<string, Type> TypesCache;

        public static void Init()
        {
            if (TypesCache != null)
            {
                return;
            }
            TypesCache = new Dictionary<string, Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.FullName != null) TypesCache[type.FullName] = type;
                }
            }
        }
        
        public static Type GetTypeByName(string name)
        {
            TypesCache.TryGetValue(name, out Type result);
            return result;
        }
    }
}
