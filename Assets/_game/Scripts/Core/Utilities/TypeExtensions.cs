using System;
using System.Collections.Generic;

namespace Core.Utilities
{
    public static class TypeExtensions
    {

        public static bool TypesDirty = false;
        public static Dictionary<string, Type> TypesCache;
        public static Type GetTypeByName(string name)
        {
            if (TypesDirty) TypesCache = null;
            if (TypesCache == null)
            {
                TypesCache = new Dictionary<string, Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly assembly in assemblies)
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (TypesCache.ContainsKey(type.FullName)) continue;
                        TypesCache.Add(type.FullName, type);
                    }
                }
            }

            TypesCache.TryGetValue(name, out Type result);
            return result;
        }
    }
}
