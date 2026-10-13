using System;
using Core.TerrainGenerator.Filters;
using UnityEngine;

namespace Runtime.Environment.Terrain
{
    /// <summary>
    /// Stores a heightmap filter on a GameObject. A biome finds its filters among its child objects.
    /// </summary>
    public class HeightmapFilterBehaviour : MonoBehaviour, IHeightmapFilterSource
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
