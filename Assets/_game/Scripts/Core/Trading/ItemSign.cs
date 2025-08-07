using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Trading
{
    [Serializable]
    public class ItemSign : IEquatable<ItemSign>
    {
        public const string LargeTag = "large";
        [SerializeField] private string id;
        [SerializeField] private string[] tags;
        [SerializeField] private int basicCost;
        [SerializeField] private float mass;
        public string Id => id;
        public IEnumerable<string> Tags => tags;
        public int BasicCost => basicCost;
        public float Mass => mass;

        public ItemSign(){}
        public ItemSign(string id, string[] tags, int basicCost, float mass)
        {
            this.id = id;
            this.tags = tags;
            this.basicCost = basicCost;
            this.mass = mass;
        }

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

        public bool Equals(ItemSign other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemSign)obj);
        }

        public override int GetHashCode()
        {
            return (id != null ? id.GetHashCode() : 0);
        }
    }
}