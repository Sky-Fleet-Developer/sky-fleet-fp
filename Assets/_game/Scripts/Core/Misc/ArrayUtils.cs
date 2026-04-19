using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Misc
{
    public static class ArrayUtils
    {
        public enum Direction { Up, Down, Match };
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

            if (highest == list.Count)
            {
                list.Add(item);
            }
            else
            {
                list.Insert(highest, item);
            }
        }

        [TestFixture(TestOf = typeof(ArrayUtils))]
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