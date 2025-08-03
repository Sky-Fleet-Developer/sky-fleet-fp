using System;

namespace Core.Configurations
{
    [Serializable]
    public struct CostRule
    {
        public float value;
        public TagCombination tags;
    }
}