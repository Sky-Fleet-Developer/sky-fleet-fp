using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Surfaces;
using Unity.Mathematics;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Produces the raw heightmap and alphamap of the visible world chunks by blending the biome sources.
    /// A chunk is composed once all source tiles it needs are loaded (or known to be missing), so it never shows
    /// half loaded data. Biomes without data at a point give their weight to the other biomes there.
    /// <para>
    /// Biomes with filters are filtered before blending (<see cref="BiomeFilterBaker"/>): the own heights of a new
    /// chunk are written first, then the filters run, then the chunks are blended from the filtered heights.
    /// Filters read across chunk edges, so with filtered biomes every chunk is blended again whenever any changes.
    /// </para>
    /// </summary>
    public class BiomeComposer : IDisposable
    {
        private const string KernelName = "CSCompose";
        private const string OwnHeightKernelName = "CSBiomeOwnHeight";
        private const int ThreadGroupSize = 8;
        private const int MaxTilesPerSiteAndChunk = 64;

        [StructLayout(LayoutKind.Sequential)]
        private struct SourceData
        {
            public const int Stride = sizeof(float) * 5 + sizeof(int) * 11;

            public Vector2 OriginMeters;
            public float TileSize;
            public float PixelsPerTile;
            public float PixelOffset;
            public Vector2Int MapMin;
            public Vector2Int MapSize;
            public int MapOffset;
            public int FilteredSlot;
            public int FilteredAlphaSlot;
            public int4 ChannelSurfaces;
        }

        private class WorldChunk
        {
            public Vector2Int Coord;
            public bool HasSlot;
            public Vector3Int SlotKey;
            public int Layer;
            public bool Composed;
            public readonly List<uint> Sites = new ();
            public readonly List<(IBiomeSource source, Vector2Int tile)> Tiles = new ();
        }

        private static readonly int ComposeHeightProperty = Shader.PropertyToID("compose_height");
        private static readonly int ComposeAlphaProperty = Shader.PropertyToID("compose_alpha");
        private static readonly int ComposeLayersProperty = Shader.PropertyToID("compose_layers");
        private static readonly int PoolHeightProperty = Shader.PropertyToID("pool_height");
        private static readonly int PoolAlphaProperty = Shader.PropertyToID("pool_alpha");
        private static readonly int PoolResolutionProperty = Shader.PropertyToID("pool_resolution");
        private static readonly int SourcesProperty = Shader.PropertyToID("biome_sources");
        private static readonly int TileMapProperty = Shader.PropertyToID("biome_tile_map");
        private static readonly int SitesProperty = Shader.PropertyToID("biome_sites");
        private static readonly int SiteIndicesProperty = Shader.PropertyToID("biome_site_indices");
        private static readonly int LayerRangesProperty = Shader.PropertyToID("biome_layer_ranges");
        private static readonly int ResolutionProperty = Shader.PropertyToID("compose_resolution");
        private static readonly int PixelsPerChunkProperty = Shader.PropertyToID("compose_pixels_per_chunk");
        private static readonly int PixelOffsetProperty = Shader.PropertyToID("compose_pixel_offset");
        private static readonly int ChunkSizeProperty = Shader.PropertyToID("compose_chunk_size");
        private static readonly int HeightRangeProperty = Shader.PropertyToID("compose_height_range");
        private static readonly int AlphaHeightRangeProperty = Shader.PropertyToID("compose_alpha_height_range");
        private static readonly int AlphaSoftnessProperty = Shader.PropertyToID("compose_alpha_softness");
        private static readonly int FilteredHeightsProperty = Shader.PropertyToID("filtered_heights");
        private static readonly int FilteredAlphaProperty = Shader.PropertyToID("filtered_alpha");
        private static readonly int OwnAlphaProperty = Shader.PropertyToID("own_alpha");
        private static readonly int FilteredLayerCountProperty = Shader.PropertyToID("filtered_layer_count");
        private static readonly int OwnHeightProperty = Shader.PropertyToID("own_height");
        private static readonly int OwnSiteRangeProperty = Shader.PropertyToID("own_site_range");

        private readonly ComputeShader _shader;
        private readonly BiomeTilePool _pool;
        private readonly BiomeFilterBaker _filterBaker = new ();
        private readonly Dictionary<Vector2Int, WorldChunk> _chunks = new ();
        private readonly List<Vector2Int> _removeCache = new ();
        private readonly List<BiomeSite> _sites = new ();
        private readonly BiomeShapeData _shapes = new ();
        private readonly List<bool> _weightedCache = new ();
        private readonly List<WorldChunk> _toCompose = new ();
        private readonly List<int4> _composeLayers = new ();
        private readonly List<int4> _ownLayers = new ();
        private readonly List<uint> _indices = new ();
        private readonly List<uint2> _ranges = new ();
        private readonly List<SourceData> _sourceData = new ();
        private readonly List<int> _tileMap = new ();
        private readonly List<(Vector2Int coord, int layer)> _readyTiles = new ();
        private ComputeBuffer _composeLayersBuffer;
        private ComputeBuffer _ownLayersBuffer;
        private ComputeBuffer _sitesBuffer;
        private ComputeBuffer _indicesBuffer;
        private ComputeBuffer _rangesBuffer;
        private ComputeBuffer _sourcesBuffer;
        private ComputeBuffer _tileMapBuffer;
        private bool _requirementsDirty = true;

        /// <summary>Raised when a source tile finished loading and pending chunks may be composed now.</summary>
        public event Action TilesLoaded;

        public BiomeComposer(ComputeShader shader, MapGpuWorker gpuWorker, int resolution, int tilePoolSize)
        {
            _shader = shader;
            _pool = new BiomeTilePool(gpuWorker, resolution, tilePoolSize);
            _pool.TileLoaded += OnTileLoaded;
        }

        private void OnTileLoaded() => TilesLoaded?.Invoke();

        /// <summary>Sets the world chunks to keep composed. Chunks that left the set release their texture slot.</summary>
        public void SetVisibleChunks(HashSet<Vector2Int> visible, TerrainData data)
        {
            _removeCache.Clear();
            foreach (var pair in _chunks)
            {
                if (!visible.Contains(pair.Key))
                {
                    _removeCache.Add(pair.Key);
                }
            }

            foreach (Vector2Int coord in _removeCache)
            {
                WorldChunk chunk = _chunks[coord];
                if (chunk.HasSlot)
                {
                    data.ReleaseChunk(chunk.SlotKey);
                }
                _chunks.Remove(coord);
                _requirementsDirty = true;
            }

            foreach (Vector2Int coord in visible)
            {
                if (!_chunks.ContainsKey(coord))
                {
                    _chunks.Add(coord, new WorldChunk { Coord = coord });
                    _requirementsDirty = true;
                }
            }
        }

        /// <summary>Recomposes every chunk, e.g. after the biomes changed. Chunks keep showing their old data until then.</summary>
        public void Invalidate()
        {
            foreach (WorldChunk chunk in _chunks.Values)
            {
                chunk.Composed = false;
            }
            _requirementsDirty = true;
        }

        /// <summary>
        /// Composes the chunks that are ready. Returns true when raw data changed.
        /// Uses HeightmapTex and AlphamapTex as intermediate targets for biome filters, so the terrain must be baked after it.
        /// </summary>
        /// <param name="heightRangeMeters">Meters represented by normalized height 1.0.</param>
        /// <param name="filtersChanged">Settings of a biome filter changed: filter and blend everything again.</param>
        /// <param name="surfaces">Surface ids the alphamap channels of the biome sources are written as.</param>
        /// <param name="edgeNoise">Bends the biome borders. A change needs <see cref="Invalidate"/>.</param>
        public bool Compose(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, float chunkSizeMeters,
            bool sharedEdgePixels, float heightRangeMeters, AlphamapSettings alphamap, bool filtersChanged,
            TerrainSurfaces surfaces, BiomeEdgeNoise edgeNoise)
        {
            if (!_shader)
            {
                return false;
            }

            BiomeDiagram.CollectSites(biomes, worldToTerrain, _sites, _shapes);
            if (_filterBaker.UpdateFilteredBiomes(biomes, data))
            {
                // Own heights of the new set of filtered biomes are missing
                foreach (WorldChunk chunk in _chunks.Values)
                {
                    chunk.Composed = false;
                }
            }

            if (_requirementsDirty)
            {
                _requirementsDirty = false;
                UpdateRequirements(biomes, chunkSizeMeters, edgeNoise.Margin);
            }

            _toCompose.Clear();
            foreach (WorldChunk chunk in _chunks.Values)
            {
                if (!chunk.Composed && IsReady(chunk) && EnsureSlot(chunk, data))
                {
                    _toCompose.Add(chunk);
                }
            }

            bool filtered = _filterBaker.Count > 0;
            if (_toCompose.Count == 0 && !(filtered && filtersChanged))
            {
                return false;
            }

            UploadBiomes(biomes, surfaces);
            if (filtered)
            {
                WriteOwnHeights(data, chunkSizeMeters, sharedEdgePixels);
                foreach (WorldChunk chunk in _toCompose)
                {
                    chunk.Composed = true;
                }

                _filterBaker.Filter(data, biomes, worldToTerrain, chunkSizeMeters, heightRangeMeters, sharedEdgePixels,
                    edgeNoise.Margin, surfaces);
                _toCompose.Clear();
                foreach (WorldChunk chunk in _chunks.Values)
                {
                    if (chunk.Composed)
                    {
                        _toCompose.Add(chunk);
                    }
                }
            }

            edgeNoise.Bind(_shader);
            Blend(data, chunkSizeMeters, sharedEdgePixels, heightRangeMeters, alphamap);
            foreach (WorldChunk chunk in _toCompose)
            {
                chunk.Composed = true;
            }
            return true;
        }

        private void UpdateRequirements(IReadOnlyList<IBiome> biomes, float chunkSizeMeters, float margin)
        {
            _pool.BeginRequirements();
            foreach (WorldChunk chunk in _chunks.Values)
            {
                var rect = new Rect(chunk.Coord.x * chunkSizeMeters, chunk.Coord.y * chunkSizeMeters, chunkSizeMeters, chunkSizeMeters);
                BiomeDiagram.SelectSites(_sites, rect, chunk.Sites, _weightedCache, margin);
                chunk.Tiles.Clear();
                foreach (uint entry in chunk.Sites)
                {
                    if ((entry & BiomeDiagram.CompetitorFlag) != 0)
                    {
                        continue;
                    }

                    BiomeSite site = _sites[(int)entry];
                    IBiomeSource source = biomes[site.Biome].Source;
                    if (source != null)
                    {
                        RequireTiles(chunk, site, source, rect);
                    }
                }
            }
            _pool.EndRequirements();
        }

        // Tiles of the source covering the chunk rectangle, rotated into the space of the site
        private void RequireTiles(WorldChunk chunk, in BiomeSite site, IBiomeSource source, Rect rect)
        {
            float tileSize = Mathf.Max(1e-3f, source.TileSizeMeters);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector2((corner & 1) == 0 ? rect.xMin : rect.xMax, (corner & 2) == 0 ? rect.yMin : rect.yMax);
                Vector2 tileSpace = site.ToLocal(point) / tileSize + source.Origin;
                min = Vector2.Min(min, tileSpace);
                max = Vector2.Max(max, tileSpace);
            }

            Vector2Int from = Vector2Int.FloorToInt(min);
            Vector2Int to = Vector2Int.FloorToInt(max);
            if ((to.x - from.x + 1) * (to.y - from.y + 1) > MaxTilesPerSiteAndChunk)
            {
                Debug.LogWarning($"Biome source tiles are too small for the terrain chunks: one chunk needs more than {MaxTilesPerSiteAndChunk} tiles");
                return;
            }

            for (int x = from.x; x <= to.x; x++)
            {
                for (int y = from.y; y <= to.y; y++)
                {
                    var tile = new Vector2Int(x, y);
                    chunk.Tiles.Add((source, tile));
                    _pool.Require(source, tile);
                }
            }
        }

        private bool IsReady(WorldChunk chunk)
        {
            foreach (var (source, tile) in chunk.Tiles)
            {
                if (!_pool.IsSettled(source, tile))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EnsureSlot(WorldChunk chunk, TerrainData data)
        {
            if (chunk.HasSlot)
            {
                return true;
            }

            if (!data.SetChunkToMap(chunk.Coord, out chunk.SlotKey, out chunk.Layer))
            {
                Debug.LogError("Has no free slot in map! Increase TerrainGenerationSettings.maxLoadedChunks value!");
                return false;
            }
            chunk.HasSlot = true;
            return true;
        }

        // Per biome: where its source lies, which pool layers hold its tiles and where its filtered heights are
        private void UploadBiomes(IReadOnlyList<IBiome> biomes, TerrainSurfaces surfaces)
        {
            _sourceData.Clear();
            _tileMap.Clear();
            for (int i = 0; i < biomes.Count; i++)
            {
                SourceData source = BuildSourceData(biomes[i].Source);
                source.FilteredSlot = _filterBaker.GetSlot(i);
                source.FilteredAlphaSlot = _filterBaker.GetAlphaSlot(i);
                source.ChannelSurfaces = surfaces.GetChannelIds(biomes[i].Source);
                _sourceData.Add(source);
            }

            Upload(ref _sitesBuffer, _sites, BiomeSite.Stride);
            Upload(ref _sourcesBuffer, _sourceData, SourceData.Stride);
            Upload(ref _tileMapBuffer, _tileMap, sizeof(int));
            _shapes.Upload();
        }

        // Own heights of every filtered biome in the chunks to compose
        private void WriteOwnHeights(TerrainData data, float chunkSizeMeters, bool sharedEdgePixels)
        {
            if (_toCompose.Count == 0)
            {
                return;
            }

            _ownLayers.Clear();
            foreach (WorldChunk chunk in _toCompose)
            {
                _ownLayers.Add(new int4(chunk.Layer, chunk.Coord.x, chunk.Coord.y, 0));
            }
            Upload(ref _ownLayersBuffer, _ownLayers, sizeof(int) * 4);

            int kernel = _shader.FindKernel(OwnHeightKernelName);
            BindCommon(kernel, data, chunkSizeMeters, sharedEdgePixels);
            _shader.SetBuffer(kernel, ComposeLayersProperty, _ownLayersBuffer);
            int groups = Mathf.CeilToInt(data.Resolution / (float)ThreadGroupSize);
            for (int slot = 0; slot < _filterBaker.Count; slot++)
            {
                // Sites of one biome are adjacent
                int biome = _filterBaker.GetBiomeIndex(slot);
                int start = _sites.FindIndex(site => site.Biome == biome);
                if (start < 0)
                {
                    continue;
                }

                int count = 0;
                while (start + count < _sites.Count && _sites[start + count].Biome == biome)
                {
                    count++;
                }

                _shader.SetTexture(kernel, OwnHeightProperty, _filterBaker.GetOwnHeightTex(slot));
                _shader.SetTexture(kernel, OwnAlphaProperty, _filterBaker.GetOwnAlphaTex(slot));
                _shader.SetInts(OwnSiteRangeProperty, start, count);
                _shader.Dispatch(kernel, groups, groups, _toCompose.Count);
            }
        }

        private void Blend(TerrainData data, float chunkSizeMeters, bool sharedEdgePixels, float heightRangeMeters,
            AlphamapSettings alphamap)
        {
            // Per composed chunk: its layer, its chunk coordinate and its range of sites
            _composeLayers.Clear();
            _indices.Clear();
            _ranges.Clear();
            foreach (WorldChunk chunk in _toCompose)
            {
                _composeLayers.Add(new int4(chunk.Layer, chunk.Coord.x, chunk.Coord.y, 0));
                _ranges.Add(new uint2((uint)_indices.Count, (uint)chunk.Sites.Count));
                _indices.AddRange(chunk.Sites);
            }

            Upload(ref _composeLayersBuffer, _composeLayers, sizeof(int) * 4);
            Upload(ref _indicesBuffer, _indices, sizeof(uint));
            Upload(ref _rangesBuffer, _ranges, sizeof(uint) * 2);

            int kernel = _shader.FindKernel(KernelName);
            BindCommon(kernel, data, chunkSizeMeters, sharedEdgePixels);
            _shader.SetTexture(kernel, ComposeHeightProperty, data.RawHeightmapTex);
            _shader.SetTexture(kernel, ComposeAlphaProperty, data.RawAlphamapTex);
            _shader.SetTexture(kernel, FilteredHeightsProperty, _filterBaker.FilteredHeights);
            _shader.SetTexture(kernel, FilteredAlphaProperty, _filterBaker.FilteredAlpha);
            _shader.SetBuffer(kernel, ComposeLayersProperty, _composeLayersBuffer);
            _shader.SetBuffer(kernel, SiteIndicesProperty, _indicesBuffer);
            _shader.SetBuffer(kernel, LayerRangesProperty, _rangesBuffer);
            _shader.SetInt(FilteredLayerCountProperty, data.LayerCount);
            _shader.SetFloat(HeightRangeProperty, heightRangeMeters);
            _shader.SetFloat(AlphaHeightRangeProperty, alphamap.HeightBlendRange);
            _shader.SetFloat(AlphaSoftnessProperty, alphamap.HeightBlendSoftness);

            int groups = Mathf.CeilToInt(data.Resolution / (float)ThreadGroupSize);
            _shader.Dispatch(kernel, groups, groups, _toCompose.Count);
        }

        // Source data and chunk layout, shared by the kernels
        private void BindCommon(int kernel, TerrainData data, float chunkSizeMeters, bool sharedEdgePixels)
        {
            _shader.SetTexture(kernel, PoolHeightProperty, _pool.HeightTex);
            _shader.SetTexture(kernel, PoolAlphaProperty, _pool.AlphaTex);
            _shader.SetBuffer(kernel, SitesProperty, _sitesBuffer);
            _shader.SetBuffer(kernel, SourcesProperty, _sourcesBuffer);
            _shader.SetBuffer(kernel, TileMapProperty, _tileMapBuffer);
            _shapes.Bind(_shader, kernel);
            _shader.SetFloat(PoolResolutionProperty, _pool.Resolution);
            _shader.SetInt(ResolutionProperty, data.Resolution);
            _shader.SetFloat(PixelsPerChunkProperty, sharedEdgePixels ? data.Resolution - 1 : data.Resolution);
            _shader.SetFloat(PixelOffsetProperty, sharedEdgePixels ? 0f : 0.5f);
            _shader.SetFloat(ChunkSizeProperty, chunkSizeMeters);
        }

        private SourceData BuildSourceData(IBiomeSource source)
        {
            if (source == null)
            {
                return new SourceData();
            }

            _readyTiles.Clear();
            _pool.GetReadyTiles(source, _readyTiles);
            float tileSize = Mathf.Max(1e-3f, source.TileSizeMeters);
            var result = new SourceData
            {
                OriginMeters = source.Origin * tileSize,
                TileSize = tileSize,
                PixelsPerTile = source.SharedEdgePixels ? _pool.Resolution - 1 : _pool.Resolution,
                PixelOffset = source.SharedEdgePixels ? 0f : 0.5f,
                MapOffset = _tileMap.Count,
            };
            if (_readyTiles.Count == 0)
            {
                return result;
            }

            Vector2Int min = _readyTiles[0].coord;
            Vector2Int max = min;
            foreach (var (coord, _) in _readyTiles)
            {
                min = Vector2Int.Min(min, coord);
                max = Vector2Int.Max(max, coord);
            }

            Vector2Int size = max - min + Vector2Int.one;
            for (int i = 0; i < size.x * size.y; i++)
            {
                _tileMap.Add(-1);
            }

            foreach (var (coord, layer) in _readyTiles)
            {
                Vector2Int local = coord - min;
                _tileMap[result.MapOffset + local.x * size.y + local.y] = layer;
            }

            result.MapMin = min;
            result.MapSize = size;
            return result;
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

        /// <summary>Releases the texture slots of all chunks.</summary>
        public void Clear(TerrainData data)
        {
            foreach (WorldChunk chunk in _chunks.Values)
            {
                if (chunk.HasSlot)
                {
                    data?.ReleaseChunk(chunk.SlotKey);
                }
            }
            _chunks.Clear();
            _requirementsDirty = true;
        }

        public void Dispose()
        {
            _pool.TileLoaded -= OnTileLoaded;
            _pool.Dispose();
            _composeLayersBuffer?.Dispose();
            _ownLayersBuffer?.Dispose();
            _filterBaker.Dispose();
            _shapes.Dispose();
            _sitesBuffer?.Dispose();
            _indicesBuffer?.Dispose();
            _rangesBuffer?.Dispose();
            _sourcesBuffer?.Dispose();
            _tileMapBuffer?.Dispose();
        }
    }
}
