using System;
using Core.Items;
using Core.Trading;

namespace Core.Configurations
{
    [Serializable]
    /// <summary>
    /// "OR" operator for item search
    /// </summary> 
    public struct TagCombination
    {
        public string[] tags;
        public bool IsEmpty => tags.Length == 0;
        public bool IsItemMatch(ItemSign item)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (!item.HasTag(tags[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}