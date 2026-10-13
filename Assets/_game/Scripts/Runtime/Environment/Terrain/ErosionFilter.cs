using System;
using Core.TerrainGenerator.Filters;
using UnityEngine;

namespace Runtime.Environment.Terrain
{
    /// <summary>
    /// Single-pass erosion (Phacelle noise gullies) baked into the heightmap.
    /// Horizontal parameters are in chunk units (1.0 = heightmap chunk size); slopes are geometric,
    /// so results stay the same if chunk size or height range change together.
    /// </summary>
    public class ErosionFilter : ScriptableObject, IHeightmapFilter
    {
        private static readonly int ScaleProperty = Shader.PropertyToID("erosion_scale");
        private static readonly int StrengthProperty = Shader.PropertyToID("erosion_strength");
        private static readonly int GullyWeightProperty = Shader.PropertyToID("erosion_gully_weight");
        private static readonly int DetailProperty = Shader.PropertyToID("erosion_detail");
        private static readonly int RoundingProperty = Shader.PropertyToID("erosion_rounding");
        private static readonly int OnsetProperty = Shader.PropertyToID("erosion_onset");
        private static readonly int AssumedSlopeProperty = Shader.PropertyToID("erosion_assumed_slope");
        private static readonly int CellScaleProperty = Shader.PropertyToID("erosion_cell_scale");
        private static readonly int NormalizationProperty = Shader.PropertyToID("erosion_normalization");
        private static readonly int OctavesProperty = Shader.PropertyToID("erosion_octaves");
        private static readonly int LacunarityProperty = Shader.PropertyToID("erosion_lacunarity");
        private static readonly int GainProperty = Shader.PropertyToID("erosion_gain");
        private static readonly int FadeTargetOffsetProperty = Shader.PropertyToID("erosion_fade_target_offset");

        [SerializeField] private ComputeShader shader;
        [SerializeField] private bool active = true;
        [Tooltip("Size of the largest gullies, in chunk units")]
        [SerializeField] private float scale = 0.7f;
        [SerializeField] private float strength = 0.22f;
        [SerializeField, Range(0, 1)] private float gullyWeight = 0.5f;
        [SerializeField] private float detail = 1.5f;
        [Tooltip("x: ridge rounding, y: crease rounding, z: input height multiplier, w: per-octave multiplier")]
        [SerializeField] private Vector4 rounding = new Vector4(0.1f, 0.5f, 0.1f, 2.0f);
        [SerializeField] private Vector4 onset = new Vector4(0.7f, 1.25f, 2.8f, 1.5f);
        [SerializeField] private Vector2 assumedSlope = new Vector2(0.7f, 1.0f);
        [SerializeField] private float cellScale = 0.7f;
        [SerializeField, Range(0, 1)] private float normalization = 0.5f;
        [SerializeField, Range(1, 10)] private int octaves = 5;
        [SerializeField] private float lacunarity = 2.0f;
        [SerializeField] private float gain = 0.5f;
        [Tooltip("Fade target = normalized height - offset. Shifts where erosion raises (valleys) or lowers (peaks)")]
        [SerializeField] private float fadeTargetOffset = 0.5f;

        private int _kernel = -1;
        private ComputeShader _kernelShader;

        public bool IsActive => active && shader && strength != 0 && scale != 0;
        public event Action Changed;

        public void Apply(in HeightmapFilterContext context)
        {
            if (_kernelShader != shader)
            {
                _kernelShader = shader;
                _kernel = shader.FindKernel("CSErode");
            }

            shader.SetFloat(ScaleProperty, scale);
            shader.SetFloat(StrengthProperty, strength);
            shader.SetFloat(GullyWeightProperty, gullyWeight);
            shader.SetFloat(DetailProperty, detail);
            shader.SetVector(RoundingProperty, rounding);
            shader.SetVector(OnsetProperty, onset);
            shader.SetVector(AssumedSlopeProperty, assumedSlope);
            shader.SetFloat(CellScaleProperty, cellScale);
            shader.SetFloat(NormalizationProperty, normalization);
            shader.SetInt(OctavesProperty, octaves);
            shader.SetFloat(LacunarityProperty, lacunarity);
            shader.SetFloat(GainProperty, gain);
            shader.SetFloat(FadeTargetOffsetProperty, fadeTargetOffset);
            context.Dispatch(shader, _kernel);
        }

        private void OnValidate() => Changed?.Invoke();
    }
}
