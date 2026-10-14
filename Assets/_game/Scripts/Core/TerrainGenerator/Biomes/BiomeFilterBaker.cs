using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Filters;
using Unity.Mathematics;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Runs the filters of every biome over the raw heightmap, before the global filters.
    /// Each biome's filters run on the blended heights and their result is mixed in by the normalized biome weight,
    /// so they fade out across the blend zone like the biome itself. Biomes are applied in blending order.
    /// Layers where a biome has no influence are skipped: filters see them as unused layers
    /// (HeightmapFilterCommon.hlsl), while neighbour reads still find their current heights.
    /// </summary>
    public class BiomeFilterBaker : IDisposable
    {
        private const string ShaderPath = "TerrainFilters/BiomeFilterBlend";
        private const string KernelName = "CSBiomeFilterBlend";
        private const int ThreadGroupSize = 8;

        private static readonly int ResultProperty = Shader.PropertyToID("blend_result");
        private static readonly int BeforeProperty = Shader.PropertyToID("blend_before");
        private static readonly int FilteredProperty = Shader.PropertyToID("blend_filtered");
        private static readonly int SlotCoordsProperty = Shader.PropertyToID("blend_slot_coords");
        private static readonly int BiomeProperty = Shader.PropertyToID("blend_biome");
        private static readonly int ResolutionProperty = Shader.PropertyToID("blend_resolution");
        private static readonly int PixelsPerChunkProperty = Shader.PropertyToID("blend_pixels_per_chunk");
        private static readonly int PixelOffsetProperty = Shader.PropertyToID("blend_pixel_offset");
        private static readonly int ChunkSizeProperty = Shader.PropertyToID("blend_chunk_size");

        private static ComputeShader _shader;
        private static int _kernel = -1;

        private readonly BiomeLayerSites _layerSites = new ();
        private readonly List<IHeightmapFilter> _activeFilters = new ();
        // One per filtered biome of a bake, so the dispatches queued for one biome keep their own content
        private readonly List<ComputeBuffer> _slotCoordsBuffers = new ();
        private int3[] _slotCoords;
        private RenderTexture _resultTex;

        /// <summary>
        /// Returns the heights with the biome filters applied, or <see cref="TerrainData.RawHeightmapTex"/>
        /// when no biome filter ran. Uses HeightmapTex and the scratch heightmap as intermediate targets,
        /// so the global filters must run after it.
        /// </summary>
        public RenderTexture Bake(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain,
            float chunkSizeMeters, float heightRangeMeters, bool sharedEdgePixels)
        {
            if (!HasActiveFilters(biomes) || !GetShader()
                || !_layerSites.Build(data, biomes, worldToTerrain, chunkSizeMeters))
            {
                return data.RawHeightmapTex;
            }

            EnsureResultTexture(data);
            Graphics.CopyTexture(data.RawHeightmapTex, _resultTex);
            _layerSites.Upload();
            _layerSites.Bind(_shader, _kernel);

            ComputeBuffer map = data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);
            RenderTexture final = data.HeightmapTex;
            int usedBuffers = 0;
            for (int biome = 0; biome < biomes.Count; biome++)
            {
                CollectActiveFilters(biomes[biome]);
                if (_activeFilters.Count == 0 || !BuildSlotCoords(data, biome))
                {
                    continue;
                }

                ComputeBuffer slotCoords = GetSlotCoordsBuffer(usedBuffers++, data.LayerCount);
                slotCoords.SetData(_slotCoords);

                // Filters write only the layers of the biome, but read neighbour layers from their source:
                // every texture used as a source must hold the current heights there
                RenderTexture scratch = _activeFilters.Count > 1 ? data.GetScratchHeightmapTex() : null;
                if (_activeFilters.Count > 1)
                {
                    Graphics.CopyTexture(_resultTex, final);
                }
                if (_activeFilters.Count > 2)
                {
                    Graphics.CopyTexture(_resultTex, scratch);
                }

                RenderTexture source = _resultTex;
                RenderTexture destination = final;
                foreach (IHeightmapFilter filter in _activeFilters)
                {
                    var context = new HeightmapFilterContext(source, destination, slotCoords, map, mapMin, mapSize,
                        data.LayerCount, data.Resolution, chunkSizeMeters, heightRangeMeters, sharedEdgePixels);
                    filter.Apply(context);
                    source = destination;
                    destination = destination == final ? scratch : final;
                }

                // The blend reads the current heights from a copy: the result texture can't be read where it's written
                RenderTexture before = source == final ? data.GetScratchHeightmapTex() : final;
                Graphics.CopyTexture(_resultTex, before);
                Blend(data, before, source, slotCoords, biome, chunkSizeMeters, sharedEdgePixels);
            }

            return usedBuffers > 0 ? _resultTex : data.RawHeightmapTex;
        }

        private static bool HasActiveFilters(IReadOnlyList<IBiome> biomes)
        {
            if (biomes == null)
            {
                return false;
            }

            foreach (IBiome biome in biomes)
            {
                IReadOnlyList<IHeightmapFilterSource> filters = biome.Filters;
                if (filters == null)
                {
                    continue;
                }

                foreach (IHeightmapFilterSource source in filters)
                {
                    if (source?.Filter != null && source.Filter.IsActive)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CollectActiveFilters(IBiome biome)
        {
            _activeFilters.Clear();
            IReadOnlyList<IHeightmapFilterSource> filters = biome.Filters;
            if (filters == null)
            {
                return;
            }

            foreach (IHeightmapFilterSource source in filters)
            {
                // Filters are read at bake time: a source may get a different filter assigned
                if (source?.Filter != null && source.Filter.IsActive)
                {
                    _activeFilters.Add(source.Filter);
                }
            }
        }

        /// <returns>False when the biome has no influence on any loaded layer.</returns>
        private bool BuildSlotCoords(TerrainData data, int biome)
        {
            if (_slotCoords == null || _slotCoords.Length != data.LayerCount)
            {
                _slotCoords = new int3[data.LayerCount];
            }

            bool any = false;
            for (int layer = 0; layer < data.LayerCount; layer++)
            {
                int3 slot = data.GetSlotCoords(layer);
                if (slot.z != 0 && !_layerSites.HasInfluence(layer, biome))
                {
                    slot.z = 0;
                }
                _slotCoords[layer] = slot;
                any |= slot.z != 0;
            }
            return any;
        }

        private void Blend(TerrainData data, RenderTexture before, RenderTexture filtered, ComputeBuffer slotCoords, int biome,
            float chunkSizeMeters, bool sharedEdgePixels)
        {
            _shader.SetTexture(_kernel, ResultProperty, _resultTex);
            _shader.SetTexture(_kernel, BeforeProperty, before);
            _shader.SetTexture(_kernel, FilteredProperty, filtered);
            _shader.SetBuffer(_kernel, SlotCoordsProperty, slotCoords);
            _shader.SetInt(BiomeProperty, biome);
            _shader.SetInt(ResolutionProperty, data.Resolution);
            _shader.SetFloat(PixelsPerChunkProperty, sharedEdgePixels ? data.Resolution - 1 : data.Resolution);
            _shader.SetFloat(PixelOffsetProperty, sharedEdgePixels ? 0f : 0.5f);
            _shader.SetFloat(ChunkSizeProperty, chunkSizeMeters);

            int groups = Mathf.CeilToInt(data.Resolution / (float)ThreadGroupSize);
            _shader.Dispatch(_kernel, groups, groups, data.LayerCount);
        }

        private ComputeBuffer GetSlotCoordsBuffer(int index, int layerCount)
        {
            if (index == _slotCoordsBuffers.Count)
            {
                _slotCoordsBuffers.Add(null);
            }

            ComputeBuffer buffer = _slotCoordsBuffers[index];
            if (buffer == null || buffer.count != layerCount)
            {
                buffer?.Dispose();
                buffer = new ComputeBuffer(layerCount, sizeof(int) * 3);
                _slotCoordsBuffers[index] = buffer;
            }
            return buffer;
        }

        private void EnsureResultTexture(TerrainData data)
        {
            RenderTexture raw = data.RawHeightmapTex;
            if (_resultTex && _resultTex.width == raw.width && _resultTex.volumeDepth == raw.volumeDepth)
            {
                return;
            }

            ReleaseResultTexture();
            _resultTex = new RenderTexture(raw.descriptor)
            {
                filterMode = raw.filterMode,
                name = "Biome filtered heightmap",
            };
            _resultTex.Create();
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Biome filters: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _kernel = _shader.FindKernel(KernelName);
            }
            return _shader;
        }

        private void ReleaseResultTexture()
        {
            if (!_resultTex)
            {
                return;
            }

            _resultTex.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_resultTex);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_resultTex);
            }
            _resultTex = null;
        }

        public void Dispose()
        {
            ReleaseResultTexture();
            _layerSites.Dispose();
            foreach (ComputeBuffer buffer in _slotCoordsBuffers)
            {
                buffer?.Dispose();
            }
            _slotCoordsBuffers.Clear();
        }
    }
}
