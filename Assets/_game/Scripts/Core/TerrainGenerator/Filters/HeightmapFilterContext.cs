using UnityEngine;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// Everything a heightmap filter needs to process all loaded chunks in one dispatch.
    /// Heights are normalized: 1 equals <see cref="HeightRangeMeters"/>.
    /// Texture pixel (x, y) of a chunk lies at world (x, z) = (chunk + (pixel + offset) / pixelsPerChunk) * chunk size,
    /// see <see cref="SharedEdgePixels"/>.
    /// </summary>
    public readonly struct HeightmapFilterContext
    {
        private static readonly int FilterSource = Shader.PropertyToID("filter_source");
        private static readonly int FilterDestination = Shader.PropertyToID("filter_destination");
        private static readonly int FilterSlotCoords = Shader.PropertyToID("filter_slot_coords");
        private static readonly int FilterMap = Shader.PropertyToID("filter_map");
        private static readonly int FilterMapMin = Shader.PropertyToID("filter_map_min");
        private static readonly int FilterMapSize = Shader.PropertyToID("filter_map_size");
        private static readonly int FilterResolution = Shader.PropertyToID("filter_resolution");
        private static readonly int FilterChunkSize = Shader.PropertyToID("filter_chunk_size");
        private static readonly int FilterHeightRange = Shader.PropertyToID("filter_height_range");
        private static readonly int FilterPixelsPerChunk = Shader.PropertyToID("filter_pixels_per_chunk");
        private static readonly int FilterPixelOffset = Shader.PropertyToID("filter_pixel_offset");
        private static readonly int FilterAlphaSource = Shader.PropertyToID("filter_alpha_source");
        private static readonly int FilterAlphaDestination = Shader.PropertyToID("filter_alpha_destination");

        /// <summary>Texture2DArray to read heights from.</summary>
        public readonly RenderTexture Source;
        /// <summary>Texture2DArray to write heights to.</summary>
        public readonly RenderTexture Destination;
        /// <summary>Texture2DArray RGBA to read the alphamap (surface weights) from. Same layers and pixels as the heights.</summary>
        public readonly RenderTexture AlphaSource;
        /// <summary>Texture2DArray RGBA to write the alphamap to; null unless the filter writes the alphamap.</summary>
        public readonly RenderTexture AlphaDestination;
        /// <summary>Per layer: int3(chunk.x, chunk.y, isUsed).</summary>
        public readonly ComputeBuffer SlotCoords;
        /// <summary>Chunk coordinate (relative to <see cref="MapMin"/>) to layer, -1 when not loaded.</summary>
        public readonly ComputeBuffer Map;
        public readonly Vector2Int MapMin;
        public readonly int MapSize;
        public readonly int LayerCount;
        public readonly int Resolution;
        public readonly float ChunkSizeMeters;
        public readonly float HeightRangeMeters;
        /// <summary>
        /// True: edge pixels are shared with neighbours, pixel i lies at i / (resolution - 1).
        /// False: pixel i is a cell centered at (i + 0.5) / resolution.
        /// </summary>
        public readonly bool SharedEdgePixels;

        public HeightmapFilterContext(RenderTexture source, RenderTexture destination, RenderTexture alphaSource,
            RenderTexture alphaDestination, ComputeBuffer slotCoords, ComputeBuffer map, Vector2Int mapMin, int mapSize,
            int layerCount, int resolution, float chunkSizeMeters, float heightRangeMeters, bool sharedEdgePixels)
        {
            Source = source;
            Destination = destination;
            AlphaSource = alphaSource;
            AlphaDestination = alphaDestination;
            SlotCoords = slotCoords;
            Map = map;
            MapMin = mapMin;
            MapSize = mapSize;
            LayerCount = layerCount;
            Resolution = resolution;
            ChunkSizeMeters = chunkSizeMeters;
            HeightRangeMeters = heightRangeMeters;
            SharedEdgePixels = sharedEdgePixels;
        }

        /// <summary>Binds the common filter_* parameters declared in HeightmapFilterCommon.hlsl.</summary>
        public void Bind(ComputeShader shader, int kernel)
        {
            shader.SetTexture(kernel, FilterSource, Source);
            shader.SetTexture(kernel, FilterDestination, Destination);
            shader.SetBuffer(kernel, FilterSlotCoords, SlotCoords);
            shader.SetBuffer(kernel, FilterMap, Map);
            shader.SetInts(FilterMapMin, MapMin.x, MapMin.y);
            shader.SetInt(FilterMapSize, MapSize);
            shader.SetInt(FilterResolution, Resolution);
            shader.SetFloat(FilterChunkSize, ChunkSizeMeters);
            shader.SetFloat(FilterHeightRange, HeightRangeMeters);
            shader.SetInt(FilterPixelsPerChunk, SharedEdgePixels ? Resolution - 1 : Resolution);
            shader.SetFloat(FilterPixelOffset, SharedEdgePixels ? 0f : 0.5f);
        }

        /// <summary>
        /// Binds the alphamap declared in HeightmapFilterAlphamap.hlsl: the source always,
        /// the destination when the filter writes the alphamap.
        /// </summary>
        public void BindAlphamap(ComputeShader shader, int kernel)
        {
            shader.SetTexture(kernel, FilterAlphaSource, AlphaSource);
            if (AlphaDestination)
            {
                shader.SetTexture(kernel, FilterAlphaDestination, AlphaDestination);
            }
        }

        /// <summary>Binds the common parameters and dispatches one thread per pixel of every layer.</summary>
        public void Dispatch(ComputeShader shader, int kernel)
        {
            Bind(shader, kernel);
            shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
            shader.Dispatch(kernel,
                Mathf.CeilToInt(Resolution / (float)x),
                Mathf.CeilToInt(Resolution / (float)y),
                Mathf.CeilToInt(LayerCount / (float)z));
        }
    }
}
