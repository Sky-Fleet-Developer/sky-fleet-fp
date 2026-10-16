using System;
using Core.TerrainGenerator.Filters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.Terrain
{
    /// <summary>
    /// Turns slopes into flat steps every <see cref="interval"/> meters of height.
    /// Within a step the input heights map to a flat floor followed by a riser up to the next floor.
    /// The riser is made of the bottom bevel (concave fillet at the floor) and the top bevel (rounded edge of the
    /// next floor), both as fractions of the interval; with both at zero the steps are sharp.
    /// Level noise shifts the step levels by position, so the floors are not horizontal.
    /// </summary>
    [Serializable]
    public class TerraceFilter : IHeightmapFilter
    {
        private const string ShaderPath = "TerrainFilters/TerraceFilter";
        private const string KernelName = "CSTerrace";

        private static readonly int IntervalProperty = Shader.PropertyToID("terrace_interval");
        private static readonly int OffsetProperty = Shader.PropertyToID("terrace_offset");
        private static readonly int TopBevelProperty = Shader.PropertyToID("terrace_top_bevel");
        private static readonly int BottomBevelProperty = Shader.PropertyToID("terrace_bottom_bevel");
        private static readonly int StrengthProperty = Shader.PropertyToID("terrace_strength");
        private static readonly int NoiseAmplitudeProperty = Shader.PropertyToID("terrace_noise_amplitude");
        private static readonly int NoiseScaleProperty = Shader.PropertyToID("terrace_noise_scale");
        private static readonly int NoiseOctavesProperty = Shader.PropertyToID("terrace_noise_octaves");
        private static readonly int NoiseSeedOffsetProperty = Shader.PropertyToID("terrace_noise_seed_offset");

        private static ComputeShader _shader;
        private static int _kernel = -1;

        [SerializeField] private bool active = true;
        [PropertyTooltip("Height of one step, in meters")]
        [SerializeField, MinValue(0.01f)] private float interval = 10f;
        [PropertyTooltip("Shifts the steps up, in meters")]
        [SerializeField] private float offset;
        [PropertyTooltip("Rounding of the step edge (convex corner), as a fraction of the interval")]
        [SerializeField, PropertyRange(0f, 1f)] private float topBevel = 0.2f;
        [PropertyTooltip("Rounding where the riser meets the floor (concave corner), as a fraction of the interval")]
        [SerializeField, PropertyRange(0f, 1f)] private float bottomBevel = 0.2f;
        [PropertyTooltip("Blend between the original (0) and terraced (1) heights")]
        [SerializeField, PropertyRange(0f, 1f)] private float strength = 1f;

        [BoxGroup("Level noise")]
        [PropertyTooltip("Maximum shift of the step levels up or down, in meters. Makes the floors tilt and wander")]
        [SerializeField, MinValue(0f)] private float levelNoiseAmplitude;
        [BoxGroup("Level noise")]
        [PropertyTooltip("Wavelength of the largest noise octave, in chunk units")]
        [SerializeField, MinValue(0.01f)] private float levelNoiseScale = 1f;
        [BoxGroup("Level noise")]
        [SerializeField, PropertyRange(1, 6)] private int levelNoiseOctaves = 2;
        [BoxGroup("Level noise")]
        [SerializeField] private int levelNoiseSeed;

        public bool IsActive => active && interval > 0 && strength > 0 && GetShader();
        public bool WritesAlphamap => false;

        public void Apply(in HeightmapFilterContext context)
        {
            // Bevels together can't exceed one interval: scale both down, keeping their ratio
            float bevelSum = topBevel + bottomBevel;
            float bevelScale = bevelSum > 1f ? 1f / bevelSum : 1f;

            ComputeShader shader = GetShader();
            shader.SetFloat(IntervalProperty, interval / context.HeightRangeMeters);
            shader.SetFloat(OffsetProperty, offset / context.HeightRangeMeters);
            shader.SetFloat(TopBevelProperty, topBevel * bevelScale);
            shader.SetFloat(BottomBevelProperty, bottomBevel * bevelScale);
            shader.SetFloat(StrengthProperty, strength);
            shader.SetFloat(NoiseAmplitudeProperty, levelNoiseAmplitude / context.HeightRangeMeters);
            shader.SetFloat(NoiseScaleProperty, levelNoiseScale);
            shader.SetInt(NoiseOctavesProperty, levelNoiseOctaves);
            shader.SetVector(NoiseSeedOffsetProperty, GetSeedOffset(levelNoiseSeed));
            context.Dispatch(shader, _kernel);
        }

        /// <summary>Noise space offset. Small enough to keep float precision.</summary>
        private static Vector2 GetSeedOffset(int seed)
        {
            var random = new System.Random(seed);
            return new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Terrace filter: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _kernel = _shader.FindKernel(KernelName);
            }
            return _shader;
        }
    }
}
