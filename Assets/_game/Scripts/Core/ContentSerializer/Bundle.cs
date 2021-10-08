using System.Collections.Generic;
using UnityEngine;

namespace Core.ContentSerializer
{
    [System.Serializable]
    public class Bundle
    {
        public string name;
        public int id;
        public List<string> tags = new List<string>();

        public Bundle()
        {
        }
    }
}
