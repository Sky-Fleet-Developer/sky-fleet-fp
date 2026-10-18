using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Filters;
using Core.TerrainGenerator.Surfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.Terrain
{
    /// <summary>
    /// Paints a surface type by the height of the biome: a height at or below <see cref="heightMin"/> paints nothing, at or
    /// above <see cref="heightMax"/> the surface fully, in between by the curve. With a noise biome source the height is
    /// the noise shaped by its curve, so the surface weight follows it; a later filter, the spawners and the terrain
    /// material read that weight (e.g. the age of the trees is the weight of their surface).
    /// The surface is mixed into what the biome paints already, so the biome needs a ground surface under it. Only the
    /// filters of biomes can paint a surface.
    /// </summary>
    [Serializable]
    public class SurfaceFromHeightFilter : IHeightmapFilter, IHeightmapFilterSurfaces
    {
        private const string ShaderPath = "TerrainFilters/SurfaceFromHeightFilter";
        private const string KernelName = "CSSurfaceFromHeight";
        private const int CurveSamples = 64;

        private static readonly int SurfaceIdProperty = Shader.PropertyToID("surface_id");
        private static readonly int HeightMinProperty = Shader.PropertyToID("surface_height_min");
        private static readonly int HeightMaxProperty = Shader.PropertyToID("surface_height_max");
        private static readonly int StrengthProperty = Shader.PropertyToID("surface_strength");
        private static readonly int KeepHeightProperty = Shader.PropertyToID("surface_keep_height");
        private static readonly int CurveProperty = Shader.PropertyToID("surface_curve");

        private static ComputeShader _shader;
        private static int _kernel = -1;
        private static readonly Vector4[] CurveBuffer = new Vector4[CurveSamples / 4];

        [SerializeField] private bool active = true;
        [SerializeField] private SurfaceType surface;
        [PropertyTooltip("Normalized height (1 = the height range of the terrain, including the height of the biome " +
                 "object) where the surface starts")]
        [SerializeField, PropertyRange(0, 1)] private float heightMin;
        [PropertyTooltip("Normalized height where the surface is at full weight")]
        [SerializeField, PropertyRange(0, 1)] private float heightMax = 0.02f;
        [PropertyTooltip("Weight of the surface over the height between the two: horizontal 0 at the start, 1 at the end")]
        [SerializeField] private AnimationCurve weightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [PropertyTooltip("Scales the weight: 1 is the curve as it is")]
        [SerializeField, PropertyRange(0, 1)] private float strength = 1f;
        [PropertyTooltip("Multiplies the heights after reading them. 0 flattens the biome when the noise is only a map, " +
                 "1 keeps the relief. The biome object height stays")]
        [SerializeField, PropertyRange(0, 1)] private float keepHeight = 1f;

        public bool IsActive => active && surface && GetShader();
        public bool WritesAlphamap => true;
        public IEnumerable<SurfaceType> Surfaces => new[] { surface };

        public void Apply(in HeightmapFilterContext context)
        {
            ComputeShader shader = GetShader();
            // A surface the terrain doesn't know yet (ids are collected on the next refresh) paints nothing
            int id = context.GetSurfaceId(surface);
            FillCurve();
            shader.SetInt(SurfaceIdProperty, id);
            shader.SetFloat(HeightMinProperty, heightMin);
            shader.SetFloat(HeightMaxProperty, Mathf.Max(heightMax, heightMin + 1e-5f));
            shader.SetFloat(StrengthProperty, id >= 0 ? strength : 0f);
            shader.SetFloat(KeepHeightProperty, keepHeight);
            shader.SetVectorArray(CurveProperty, CurveBuffer);
            context.BindAlphamap(shader, _kernel);
            context.Dispatch(shader, _kernel);
        }

        private void FillCurve()
        {
            AnimationCurve source = weightCurve != null && weightCurve.length > 0 ? weightCurve : AnimationCurve.Linear(0, 0, 1, 1);
            for (int i = 0; i < CurveSamples; i += 4)
            {
                var packed = new Vector4();
                for (int k = 0; k < 4; k++)
                {
                    packed[k] = Mathf.Clamp01(source.Evaluate((i + k) / (CurveSamples - 1f)));
                }
                CurveBuffer[i / 4] = packed;
            }
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Surface from height filter: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _kernel = _shader.FindKernel(KernelName);
            }
            return _shader;
        }
    }
}
