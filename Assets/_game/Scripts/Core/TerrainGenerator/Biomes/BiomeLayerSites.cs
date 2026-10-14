using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// The biome sites that matter in every texture layer of <see cref="TerrainData"/>, uploaded as the
    /// biome_sites / biome_site_indices / biome_layer_ranges buffers of BiomeDiagram.hlsl, indexed by layer.
    /// </summary>
    public class BiomeLayerSites : IDisposable
    {
        private static readonly int SitesProperty = Shader.PropertyToID("biome_sites");
        private static readonly int SiteIndicesProperty = Shader.PropertyToID("biome_site_indices");
        private static readonly int LayerRangesProperty = Shader.PropertyToID("biome_layer_ranges");

        private readonly List<BiomeSite> _sites = new ();
        private readonly List<uint> _indices = new ();
        private readonly List<uint> _layerIndices = new ();
        private readonly List<bool> _weightedCache = new ();
        private uint2[] _layerRanges;
        private ComputeBuffer _sitesBuffer;
        private ComputeBuffer _indicesBuffer;
        private ComputeBuffer _layerRangesBuffer;

        public IReadOnlyList<BiomeSite> Sites => _sites;

        /// <param name="margin">How far biome borders may be bent, see <see cref="Settings.BiomeEdgeNoise.Margin"/>.</param>
        /// <returns>False when there are no sites.</returns>
        public bool Build(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, float chunkSizeMeters,
            float margin)
        {
            BiomeDiagram.CollectSites(biomes, worldToTerrain, _sites);
            if (_sites.Count == 0)
            {
                return false;
            }

            // Keep the slot coords on the CPU in sync with the GPU copy the kernels read
            data.FlushMap();
            if (_layerRanges == null || _layerRanges.Length != data.LayerCount)
            {
                _layerRanges = new uint2[data.LayerCount];
            }

            _indices.Clear();
            for (int layer = 0; layer < data.LayerCount; layer++)
            {
                int3 slot = data.GetSlotCoords(layer);
                if (slot.z == 0)
                {
                    _layerRanges[layer] = uint2.zero;
                    continue;
                }

                var rect = new Rect(slot.x * chunkSizeMeters, slot.y * chunkSizeMeters, chunkSizeMeters, chunkSizeMeters);
                BiomeDiagram.SelectSites(_sites, rect, _layerIndices, _weightedCache, margin);
                _layerRanges[layer] = new uint2((uint)_indices.Count, (uint)_layerIndices.Count);
                _indices.AddRange(_layerIndices);
            }
            return true;
        }

        /// <summary>True when the biome can have weight above zero somewhere in the layer.</summary>
        public bool HasInfluence(int layer, int biome)
        {
            uint2 range = _layerRanges[layer];
            for (uint i = 0; i < range.y; i++)
            {
                uint entry = _indices[(int)(range.x + i)];
                if ((entry & BiomeDiagram.CompetitorFlag) == 0 && _sites[(int)entry].Biome == biome)
                {
                    return true;
                }
            }
            return false;
        }

        public void Upload()
        {
            BufferUtility.Upload(ref _sitesBuffer, _sites, BiomeSite.Stride);
            BufferUtility.Upload(ref _indicesBuffer, _indices, sizeof(uint));
            if (_layerRangesBuffer == null || _layerRangesBuffer.count != _layerRanges.Length)
            {
                _layerRangesBuffer?.Dispose();
                _layerRangesBuffer = new ComputeBuffer(_layerRanges.Length, sizeof(uint) * 2);
            }
            _layerRangesBuffer.SetData(_layerRanges);
        }

        public void Bind(ComputeShader shader, int kernel)
        {
            shader.SetBuffer(kernel, SitesProperty, _sitesBuffer);
            shader.SetBuffer(kernel, SiteIndicesProperty, _indicesBuffer);
            shader.SetBuffer(kernel, LayerRangesProperty, _layerRangesBuffer);
        }

        public void Dispose()
        {
            _sitesBuffer?.Dispose();
            _indicesBuffer?.Dispose();
            _layerRangesBuffer?.Dispose();
            _sitesBuffer = null;
            _indicesBuffer = null;
            _layerRangesBuffer = null;
        }
    }

    internal static class BufferUtility
    {
        // Buffers grow to fit and never shrink; a buffer can't be empty, so it holds at least one element
        public static void Upload<T>(ref ComputeBuffer buffer, List<T> content, int stride) where T : struct
        {
            int count = Mathf.Max(1, content.Count);
            if (buffer == null || buffer.count < count)
            {
                buffer?.Dispose();
                buffer = new ComputeBuffer(Mathf.NextPowerOfTwo(count), stride);
            }

            if (content.Count > 0)
            {
                buffer.SetData(content);
            }
        }
    }
}
