using System;
using Core.Configurations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Items
{
    [System.Serializable]
    public class ContainerInfo
    {
        private readonly char[] _andSymbols = { '&', ' ' }; 
        private readonly char[] _orSymbols = { '|', ' ' }; 
        [SerializeField] private string signId;
        [SerializeField] private TagCombination[] includeTags;
        [SerializeField] private TagCombination[] excludeTags;
        [SerializeField] private float maxVolume;

        public string SignId => signId;

        public ContainerInfo()
        {
        }

        public ContainerInfo(string signId, float maxVolume, string include, string exclude)
        {
            this.signId = signId;
            this.maxVolume = maxVolume;
            
            var properties = include.Split(_orSymbols, StringSplitOptions.RemoveEmptyEntries);
            includeTags = new TagCombination[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                includeTags[i].tags = properties[i].Split(_andSymbols, StringSplitOptions.RemoveEmptyEntries);
            }

            if (string.IsNullOrEmpty(exclude))
            {
                excludeTags = Array.Empty<TagCombination>();
            }
            else
            {
                properties = exclude.Split(_orSymbols, StringSplitOptions.RemoveEmptyEntries);
                excludeTags = new TagCombination[properties.Length];
                for (var i = 0; i < properties.Length; i++)
                {
                    excludeTags[i].tags = properties[i].Split(_andSymbols, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        public bool IsItemMatch(ItemInstance itemInstance, float volumeEmployed)
        {
            if (itemInstance.GetVolume() > maxVolume - volumeEmployed)
            {
                return false;
            }

            bool condition = false;
            for (var i = 0; i < includeTags.Length; i++)
            {
                if (includeTags[i].IsItemMatch(itemInstance.Sign))
                {
                    condition = true;
                    break;
                }
            }

            if (!condition)
            {
                return false;
            }
            
            for (var i = 0; i < excludeTags.Length; i++)
            {
                if (excludeTags[i].IsItemMatch(itemInstance.Sign))
                {
                    return false;
                }
            }

            return true;
        }
    }
}