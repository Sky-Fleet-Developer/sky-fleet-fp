using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Scatter;
using Core.TerrainGenerator.Surfaces;
using UnityEngine;

namespace Runtime.Scatter
{
    /// <summary>
    /// The layers of a kind of scattered detail, grouped by surface id, as ScatterPlacement.hlsl reads them to pick the
    /// layer of a candidate: scatter_layers, scatter_surface_layers, scatter_surface_count, scatter_max_density.
    /// The kind keeps its own data of the layers in a buffer of its own, in the same order (see <see cref="Build{T}"/>).
    /// </summary>
    public class ScatterLayerTable : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 7;

            public float Density;
            public Vector4 Noise;
            public uint NoiseSeed;
            public float MinWeight;
        }

        // Values of the density noise above which the given share of the ground lies, for shares 0, 0.1 .. 1
        private static readonly float[] NoiseThresholds =
            { 1.000f, 0.724f, 0.653f, 0.598f, 0.548f, 0.500f, 0.452f, 0.403f, 0.347f, 0.276f, 0.000f };

        private static readonly int LayersProperty = Shader.PropertyToID("scatter_layers");
        private static readonly int SurfaceLayersProperty = Shader.PropertyToID("scatter_surface_layers");
        private static readonly int SurfaceCountProperty = Shader.PropertyToID("scatter_surface_count");
        private static readonly int MaxDensityProperty = Shader.PropertyToID("scatter_max_density");

        private readonly List<LayerData> _data = new ();
        private GraphicsBuffer _layers;
        private GraphicsBuffer _surfaceLayers;

        /// <summary>Details per square meter of the densest surface, all its layers together. 0: nothing grows.</summary>
        public float MaxDensity { get; private set; }

        /// <summary>
        /// Lists the usable layers of every surface id. <paramref name="added"/> is called for each in order, with its
        /// surface id, so the kind can list its own data of the layer at the same index.
        /// </summary>
        public void Build<T>(IReadOnlyList<SurfaceType> surfaces, IReadOnlyList<T> layers, Action<int, T> added,
            Predicate<T> usable = null) where T : class, IScatterLayer
        {
            _data.Clear();
            int count = Mathf.Max(1, surfaces.Count);
            var ranges = new Vector2Int[count];
            MaxDensity = 0;
            for (int id = 0; id < surfaces.Count; id++)
            {
                ranges[id].x = _data.Count;
                float surfaceDensity = 0;
                foreach (T layer in layers)
                {
                    if (layer == null || !surfaces[id] || layer.Surface != surfaces[id] || layer.Density <= 0 ||
                        (layer.NoiseScale > 0 && layer.NoiseCoverage <= 0) || (usable != null && !usable(layer)))
                    {
                        continue;
                    }

                    _data.Add(new LayerData
                    {
                        Density = layer.Density,
                        Noise = NoiseOf(layer),
                        NoiseSeed = (uint)layer.NoiseSeed,
                        MinWeight = layer.MinSurfaceWeight,
                    });
                    surfaceDensity += layer.Density;
                    added(id, layer);
                }
                ranges[id].y = _data.Count - ranges[id].x;
                MaxDensity = Mathf.Max(MaxDensity, surfaceDensity);
            }

            // At least one element: every buffer a kernel reads must be bound
            if (_data.Count == 0)
            {
                _data.Add(default);
            }
            ScatterUtility.EnsureBuffer(ref _layers, _data.Count, LayerData.Stride);
            ScatterUtility.EnsureBuffer(ref _surfaceLayers, count, sizeof(int) * 2);
            _layers.SetData(_data);
            _surfaceLayers.SetData(ranges);
        }

        public void Bind(ComputeShader shader, int kernel)
        {
            shader.SetBuffer(kernel, LayersProperty, _layers);
            shader.SetBuffer(kernel, SurfaceLayersProperty, _surfaceLayers);
            shader.SetInt(SurfaceCountProperty, _surfaceLayers.count);
            shader.SetFloat(MaxDensityProperty, MaxDensity);
        }

        public void Dispose()
        {
            _layers?.Release();
            _layers = null;
            _surfaceLayers?.Release();
            _surfaceLayers = null;
        }

        // Density patches of a layer for ScatterLayerNoise: frequency, the noise values of the edges, inverted
        private static Vector4 NoiseOf(IScatterLayer layer)
        {
            if (layer.NoiseScale <= 0 || layer.NoiseCoverage >= 1)
            {
                return Vector4.zero;
            }

            float position = Mathf.Clamp01(layer.NoiseCoverage) * (NoiseThresholds.Length - 1);
            int index = Mathf.Min((int)position, NoiseThresholds.Length - 2);
            float threshold = Mathf.Lerp(NoiseThresholds[index], NoiseThresholds[index + 1], position - index);
            // Softness 1 spreads an edge over about one standard deviation of the noise each way
            float halfWidth = Mathf.Max(1e-3f, layer.NoiseSoftness * 0.17f);
            return new Vector4(1 / layer.NoiseScale, threshold - halfWidth, threshold + halfWidth, layer.NoiseInvert ? 1 : 0);
        }
    }
}
