using System;
using System.Collections.Generic;

namespace Core.Utilities
{
    public static class ListExtension
    {
        private static System.Random rng = new System.Random();  

        public static List<T> Clone<T>(this List<T> value)
        {
            List<T> ret = new List<T>(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                ret.Add(value[i]);
            }
            return ret;
        }
        public static List<T> DeepClone<T>(this List<T> value) where T : ICloneable
        {
            List<T> ret = new List<T>(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                ret.Add((T)value[i].Clone());
            }
            return ret;
        }
    
        public static T GetRandom<T>(this List<T> value)
        {
            return value[UnityEngine.Random.Range(0, value.Count)];
        }
    
        public static void Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }
    }
}
