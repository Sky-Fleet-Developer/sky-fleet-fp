using System;
using Core.TerrainGenerator.Filters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.Terrain
{
    /// <summary>
    /// Domain warping: every pixel takes the heights and the alphamap from a position displaced by fBm simplex noise,
    /// so features and surfaces bend together. Horizontal parameters are in chunk units (1.0 = heightmap chunk size).
    /// The displacement is limited to just under one chunk, since a filter can only read its direct neighbours.
    /// </summary>
    [Serializable]
    public class DomainWarpFilter : IHeightmapFilter
    {
        private const string ShaderPath = "TerrainFilters/DomainWarpFilter";
        private const string KernelName = "CSWarp";

        private static readonly int ScaleProperty = Shader.PropertyToID("warp_scale");
        private static readonly int AmplitudeProperty = Shader.PropertyToID("warp_amplitude");
        private static readonly int OctavesProperty = Shader.PropertyToID("warp_octaves");
        private static readonly int LacunarityProperty = Shader.PropertyToID("warp_lacunarity");
        private static readonly int GainProperty = Shader.PropertyToID("warp_gain");
        private static readonly int IterationsProperty = Shader.PropertyToID("warp_iterations");
        private static readonly int SeedOffsetProperty = Shader.PropertyToID("warp_seed_offset");

        private static ComputeShader _shader;
        private static int _kernel = -1;

        [SerializeField] private bool active = true;
        [PropertyTooltip("Wavelength of the largest warp octave, in chunk units")]
        [SerializeField, MinValue(0.01f)] private float scale = 0.5f;
        [PropertyTooltip("Maximum displacement, in chunk units")]
        [SerializeField, PropertyRange(0f, 0.9f)] private float amplitude = 0.1f;
        [SerializeField, PropertyRange(1, 8)] private int octaves = 3;
        [SerializeField, MinValue(1f)] private float lacunarity = 2.0f;
        [SerializeField, PropertyRange(0f, 1f)] private float gain = 0.5f;
        [PropertyTooltip("1: p + warp(p). Each further iteration warps the warp: p + warp(p + warp(p)) and so on")]
        [SerializeField, PropertyRange(1, 3)] private int iterations = 1;
        [SerializeField] private int seed;

        public bool IsActive => active && amplitude > 0 && scale > 0 && GetShader();
        public bool WritesAlphamap => true;

        public void Apply(in HeightmapFilterContext context)
        {
            ComputeShader shader = GetShader();
            shader.SetFloat(ScaleProperty, scale);
            shader.SetFloat(AmplitudeProperty, amplitude);
            shader.SetInt(OctavesProperty, octaves);
            shader.SetFloat(LacunarityProperty, lacunarity);
            shader.SetFloat(GainProperty, gain);
            shader.SetInt(IterationsProperty, iterations);
            shader.SetVector(SeedOffsetProperty, GetSeedOffset(seed));
            context.BindAlphamap(shader, _kernel);
            context.Dispatch(shader, _kernel);
        }

        /// <summary>Noise space offsets of the x (xy) and y (zw) displacement channels. Small enough to keep float precision.</summary>
        private static Vector4 GetSeedOffset(int seed)
        {
            var random = new System.Random(seed);
            return new Vector4(
                (float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f,
                (float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Domain warp filter: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _kernel = _shader.FindKernel(KernelName);
            }
            return _shader;
        }
    }
}
