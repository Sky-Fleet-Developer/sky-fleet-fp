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
        [SerializeField] private string name;
        public string Id => id;
        public IEnumerable<string> Tags => tags;
    }
}