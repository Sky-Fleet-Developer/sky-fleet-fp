using System.Collections.Generic;
using Core.TerrainGenerator.Biomes;
using Core.TerrainGenerator.Biomes.Sources;
using UnityEngine;

namespace Runtime.Environment.Terrain.Biomes
{
    /// <summary>
    /// A biome centered at this GameObject. Must be a descendant of a <see cref="BiomeSet"/>;
    /// its place in the hierarchy defines the blending order. The source data rotates with the GameObject around Y.
    /// </summary>
    public class BiomeBehaviour : MonoBehaviour, IBiome
    {
        [Tooltip("Power weight of the cell, in meters: a larger radius pushes the cell borders outwards")]
        [SerializeField, Min(0)] private float radius;
        [Tooltip("Distance from the cell border into the biome, in meters, where the biome influence reaches 100%")]
        [SerializeField, Min(0)] private float blendDistance = 500;
        [SerializeField] private Color debugColor = Color.white;
        [SerializeReference] private IBiomeSource source = new RawTileBiomeSource();

        public float Radius => radius;
        public float BlendDistance => blendDistance;
        public Color DebugColor => debugColor;
        public IBiomeSource Source => source;

        public void GetSites(List<BiomeSitePose> sites)
        {
            sites.Add(new BiomeSitePose(transform.position, transform.eulerAngles.y));
        }

        private void Reset()
        {
            debugColor = Color.HSVToRGB(Random.value, 0.7f, 0.9f);
        }
    }
}
