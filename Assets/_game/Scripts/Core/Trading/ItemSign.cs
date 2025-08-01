using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Trading
{
    [Serializable]
    public class ItemSign
    {
        [SerializeField] private string id;
        [SerializeField] private string[] tags;
        [SerializeField] private int basicCost;
        public string Id => id;
        public IEnumerable<string> Tags => tags;

        public bool HasTag(string tag)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }
            return false;
        }
        public int BasicCost => basicCost;
    }
}