using System.Collections.Generic;
using Core.TerrainGenerator.Biomes;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Filters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.Terrain.Biomes
{
    /// <summary>
    /// A biome centered at this GameObject. Must be a descendant of a <see cref="BiomeSet"/>;
    /// its place in the hierarchy defines the blending order. The source data rotates with the GameObject around Y,
    /// and the GameObject height above the terrain origin is added to the biome heights.
    /// Heightmap filters (<see cref="IHeightmapFilterSource"/> components) on this GameObject and its descendants
    /// belong to the biome, in hierarchy order; a nested biome owns the filters below it.
    /// </summary>
    public class BiomeBehaviour : MonoBehaviour, IBiome
    {
        [Tooltip("Power weight of the cell, in meters: a larger radius pushes the cell borders outwards")]
        [SerializeField, MinValue(0)] private float radius;
        [Tooltip("Distance from the cell border into the biome, in meters, where the biome influence reaches 100%")]
        [SerializeField, MinValue(0)] private float blendDistance = 500;
        [Tooltip("Normal biomes share the world; other modes make the biome a layer applied on top of them")]
        [SerializeField] private BiomeBlendMode blendMode = BiomeBlendMode.Normal;
        [SerializeField] private Color debugColor = Color.white;
        [SerializeReference] private IBiomeSource source = new RawTileBiomeSource();

        public float Radius => radius;
        public float BlendDistance => blendDistance;
        public BiomeBlendMode BlendMode => blendMode;
        public Color DebugColor => debugColor;
        public IBiomeSource Source => source;

        private readonly List<IHeightmapFilterSource> _filters = new ();
        private readonly List<IHeightmapFilterSource> _filterComponentsCache = new ();
        private readonly List<IBiome> _biomeComponentsCache = new ();

        /// <summary>Collected on every access, so the list follows the hierarchy. Reused between calls.</summary>
        public IReadOnlyList<IHeightmapFilterSource> Filters
        {
            get
            {
                _filters.Clear();
                CollectFilters(transform, _filters);
                return _filters;
            }
        }

        public void GetSites(List<BiomeSitePose> sites)
        {
            sites.Add(new BiomeSitePose(transform.position, transform.eulerAngles.y));
        }

        private void CollectFilters(Transform node, List<IHeightmapFilterSource> result)
        {
            if (!node.gameObject.activeInHierarchy)
            {
                return;
            }

            if (node != transform)
            {
                node.GetComponents(_biomeComponentsCache);
                if (_biomeComponentsCache.Count > 0)
                {
                    return;
                }
            }

            node.GetComponents(_filterComponentsCache);
            foreach (IHeightmapFilterSource filter in _filterComponentsCache)
            {
                if (filter is not Behaviour { enabled: false })
                {
                    result.Add(filter);
                }
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CollectFilters(node.GetChild(i), result);
            }
        }

        private void Reset()
        {
            debugColor = Color.HSVToRGB(Random.value, 0.7f, 0.9f);
        }
    }
}
