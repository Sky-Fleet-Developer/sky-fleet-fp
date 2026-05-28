using System;
using System.Collections.Generic;
using Core.Misc;
using NUnit.Framework;

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
        
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <param name="compare"></param>
        /// <typeparam name="T"></typeparam>
        public static void InsertByAscendingOrder<T>(this List<T> list, T item, Comparison<T> compare)
        {
            int i = 0;
            int highest = list.Count;
            int lowest = -1;
            while (i++ < 1000)
            {
                int delta = highest - lowest;
                var pointer = lowest + delta / 2;
                if (pointer == lowest)
                {
                    break;
                }
                if (delta < 1)
                {
                    break;
                }
                int direction = compare(list[pointer], item);
                if (direction < 0)
                {
                    lowest = pointer;
                }
                else if (direction > 0)
                {
                    highest = pointer;
                }
                else
                {
                    break;
                }
            }

            lock (list)
            {
                if (highest == list.Count)
                {
                    list.Add(item);
                }
                else
                {
                    list.Insert(highest, item);
                }
            }
        }

        [TestFixture(TestOf = typeof(ListExtension))]
        private class Test
        {
            [TestCase(3, 2)]
            [TestCase(-6, 0)]
            [TestCase(13, 4)]
            [TestCase(16, 5)]
            [TestCase(35, 8)]
            public void TestInsertByAscendingOrder(int item, int expected)
            {
                var source = new List<int>{ -5, 2, 8, 12, 15, 20, 25, 30 };
                
                source.InsertByAscendingOrder(item, (i1, i2) => i1.CompareTo(i2));
                
                Assert.AreEqual(expected, source.IndexOf(item));
            }
        }
    }
}
