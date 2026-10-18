using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Polygon corners and fade curve samples of the shaped biome sites, filled by <see cref="BiomeDiagram.CollectSites"/>
    /// and uploaded as the biome_points / biome_curves buffers of BiomeDiagram.hlsl.
    /// </summary>
    public class BiomeShapeData : IDisposable
    {
        private static readonly int PointsProperty = Shader.PropertyToID("biome_points");
        private static readonly int CurvesProperty = Shader.PropertyToID("biome_curves");

        private ComputeBuffer _pointsBuffer;
        private ComputeBuffer _curvesBuffer;

        /// <summary>Polygon corners in the local space of their site, per site <see cref="BiomeSite.PointStart"/>.</summary>
        public readonly List<Vector2> Points = new ();

        /// <summary><see cref="BiomeShape.CurveSamples"/> samples per site, from <see cref="BiomeSite.CurveOffset"/>.</summary>
        public readonly List<float> Curves = new ();

        public void Clear()
        {
            Points.Clear();
            Curves.Clear();
        }

        public void Upload()
        {
            BufferUtility.Upload(ref _pointsBuffer, Points, sizeof(float) * 2);
            BufferUtility.Upload(ref _curvesBuffer, Curves, sizeof(float));
        }

        public void Bind(ComputeShader shader, int kernel)
        {
            shader.SetBuffer(kernel, PointsProperty, _pointsBuffer);
            shader.SetBuffer(kernel, CurvesProperty, _curvesBuffer);
        }

        public void Dispose()
        {
            _pointsBuffer?.Dispose();
            _curvesBuffer?.Dispose();
            _pointsBuffer = null;
            _curvesBuffer = null;
        }
    }
}
