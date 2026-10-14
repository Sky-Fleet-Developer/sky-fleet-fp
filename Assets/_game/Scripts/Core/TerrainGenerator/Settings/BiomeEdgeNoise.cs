using System;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    /// <summary>
    /// Bends the borders between biomes: biome weights are evaluated at a position shifted by fractal noise
    /// (BiomeWarp in BiomeDiagram.hlsl), while the biome data is still sampled at the true position.
    /// One warp for the whole terrain, so neighbouring biomes keep fitting together.
    /// </summary>
    [Serializable]
    public class BiomeEdgeNoise
    {
        private static readonly int AmplitudeProperty = Shader.PropertyToID("biome_warp_amplitude");
        private static readonly int FrequencyProperty = Shader.PropertyToID("biome_warp_frequency");
        private static readonly int OctavesProperty = Shader.PropertyToID("biome_warp_octaves");
        private static readonly int OffsetProperty = Shader.PropertyToID("biome_warp_offset");

        [Tooltip("Largest shift of a biome border along each axis, in meters. 0 keeps the borders straight")]
        [SerializeField, Min(0)] private float amplitude = 400;
        [Tooltip("Size of the largest bends, in meters")]
        [SerializeField, Min(1)] private float wavelength = 3000;
        [Tooltip("Each octave adds bends of half the size and half the amplitude")]
        [SerializeField, Range(1, 6)] private int octaves = 3;
        [SerializeField] private int seed;

        /// <summary>
        /// Farthest a border can move, in meters: the margin by which areas are widened when looking for the
        /// biomes that matter inside them.
        /// </summary>
        public float Margin => amplitude * 1.415f;

        /// <summary>Sets the biome_warp_* parameters of BiomeDiagram.hlsl.</summary>
        public void Bind(ComputeShader shader)
        {
            var random = new System.Random(seed);
            shader.SetFloat(AmplitudeProperty, amplitude);
            shader.SetFloat(FrequencyProperty, 1f / Mathf.Max(1f, wavelength));
            shader.SetInt(OctavesProperty, Mathf.Clamp(octaves, 1, 6));
            // A small offset keeps float precision
            shader.SetVector(OffsetProperty, new Vector4((float)random.NextDouble() * 289f, (float)random.NextDouble() * 289f));
        }
    }
}
