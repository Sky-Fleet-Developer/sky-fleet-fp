using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Bakes the blended biome debug colors of every loaded chunk into <see cref="DebugTex"/>,
    /// a texture array with the same layers and pixel layout as the heightmap.
    /// </summary>
    public class BiomeDebugBaker : IDisposable
    {
        private const string KernelName = "CSBiomeDebug";
        private const int ThreadGroupSize = 8;

        private static readonly int DestinationProperty = Shader.PropertyToID("biome_debug_destination");
        private static readonly int SlotCoordsProperty = Shader.PropertyToID("biome_slot_coords");
        private static readonly int ColorsProperty = Shader.PropertyToID("biome_colors");
        private static readonly int SitesProperty = Shader.PropertyToID("biome_sites");
        private static readonly int SiteIndicesProperty = Shader.PropertyToID("biome_site_indices");
        private static readonly int LayerRangesProperty = Shader.PropertyToID("biome_layer_ranges");
        private static readonly int ResolutionProperty = Shader.PropertyToID("biome_resolution");
        private static readonly int PixelsPerChunkProperty = Shader.PropertyToID("biome_pixels_per_chunk");
        private static readonly int PixelOffsetProperty = Shader.PropertyToID("biome_pixel_offset");
        private static readonly int ChunkSizeProperty = Shader.PropertyToID("biome_chunk_size");

        private readonly ComputeShader _shader;
        private readonly List<BiomeSite> _sites = new ();
        private readonly List<Vector4> _colors = new ();
        private readonly List<uint> _indices = new ();
        private readonly List<uint> _layerIndices = new ();
        private readonly List<bool> _weightedCache = new ();
        private uint2[] _layerRanges;
        private RenderTexture _debugTex;
        private ComputeBuffer _sitesBuffer;
        private ComputeBuffer _colorsBuffer;
        private ComputeBuffer _indicesBuffer;
        private ComputeBuffer _layerRangesBuffer;

        public RenderTexture DebugTex => _debugTex;

        public BiomeDebugBaker(ComputeShader shader)
        {
            _shader = shader;
        }

        /// <returns>False when there is nothing to show: no shader or no biomes.</returns>
        public bool Bake(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, float chunkSizeMeters,
            bool sharedEdgePixels)
        {
            if (!_shader || biomes == null || biomes.Count == 0)
            {
                return false;
            }

            BiomeDiagram.CollectSites(biomes, worldToTerrain, _sites);
            if (_sites.Count == 0)
            {
                return false;
            }

            EnsureTexture(data);

            // Keep the slot coords on the CPU in sync with the GPU copy the kernel reads
            data.FlushMap();
            _layerRanges ??= new uint2[data.LayerCount];
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
                BiomeDiagram.SelectSites(_sites, rect, _layerIndices, _weightedCache);
                _layerRanges[layer] = new uint2((uint)_indices.Count, (uint)_layerIndices.Count);
                _indices.AddRange(_layerIndices);
            }

            _colors.Clear();
            foreach (IBiome biome in biomes)
            {
                Color color = biome.DebugColor.linear;
                _colors.Add(new Vector4(color.r, color.g, color.b, 1));
            }

            Upload(ref _sitesBuffer, _sites, BiomeSite.Stride);
            Upload(ref _colorsBuffer, _colors, sizeof(float) * 4);
            Upload(ref _indicesBuffer, _indices, sizeof(uint));
            if (_layerRangesBuffer == null || _layerRangesBuffer.count != _layerRanges.Length)
            {
                _layerRangesBuffer?.Dispose();
                _layerRangesBuffer = new ComputeBuffer(_layerRanges.Length, sizeof(uint) * 2);
            }
            _layerRangesBuffer.SetData(_layerRanges);

            int kernel = _shader.FindKernel(KernelName);
            _shader.SetTexture(kernel, DestinationProperty, _debugTex);
            _shader.SetBuffer(kernel, SlotCoordsProperty, data.SlotCoordsBuffer);
            _shader.SetBuffer(kernel, ColorsProperty, _colorsBuffer);
            _shader.SetBuffer(kernel, SitesProperty, _sitesBuffer);
            _shader.SetBuffer(kernel, SiteIndicesProperty, _indicesBuffer);
            _shader.SetBuffer(kernel, LayerRangesProperty, _layerRangesBuffer);
            _shader.SetInt(ResolutionProperty, data.Resolution);
            _shader.SetFloat(PixelsPerChunkProperty, sharedEdgePixels ? data.Resolution - 1 : data.Resolution);
            _shader.SetFloat(PixelOffsetProperty, sharedEdgePixels ? 0f : 0.5f);
            _shader.SetFloat(ChunkSizeProperty, chunkSizeMeters);

            int groups = Mathf.CeilToInt(data.Resolution / (float)ThreadGroupSize);
            _shader.Dispatch(kernel, groups, groups, data.LayerCount);
            return true;
        }

        private void EnsureTexture(TerrainData data)
        {
            if (_debugTex && _debugTex.width == data.Resolution && _debugTex.volumeDepth == data.LayerCount)
            {
                return;
            }

            ReleaseTexture();
            _debugTex = new RenderTexture(data.Resolution, data.Resolution, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = data.LayerCount,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Biome debug colors",
            };
            _debugTex.Create();
        }

        // Buffers grow to fit and never shrink; a buffer can't be empty, so it holds at least one element
        private static void Upload<T>(ref ComputeBuffer buffer, List<T> content, int stride) where T : struct
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

        private void ReleaseTexture()
        {
            if (!_debugTex)
            {
                return;
            }

            _debugTex.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_debugTex);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_debugTex);
            }
            _debugTex = null;
        }

        public void Dispose()
        {
            ReleaseTexture();
            _sitesBuffer?.Dispose();
            _colorsBuffer?.Dispose();
            _indicesBuffer?.Dispose();
            _layerRangesBuffer?.Dispose();
            _sitesBuffer = null;
            _colorsBuffer = null;
            _indicesBuffer = null;
            _layerRangesBuffer = null;
        }
    }
}
