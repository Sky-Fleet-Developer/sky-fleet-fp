using System;
using UnityEngine;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// Stores a heightmap filter as an asset. Global filters are child assets of <see cref="TerrainGenerationSettings"/>
    /// and run in the order they are listed there, after the biomes are blended.
    /// </summary>
    public class HeightmapFilterAsset : ScriptableObject, IHeightmapFilterSource
    {
        [SerializeReference] private IHeightmapFilter filter;

        public IHeightmapFilter Filter
        {
            get => filter;
            set
            {
                filter = value;
                Changed?.Invoke();
            }
        }

        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
