#ifndef HEIGHTMAP_FILTER_COMMON
#define HEIGHTMAP_FILTER_COMMON

// Shared contract for heightmap filter kernels. Bound by HeightmapFilterContext.Bind.
// Dispatch layout: id.xy = pixel, id.z = texture array layer.
// Pixel i lies at chunk uv (i + filter_pixel_offset) / filter_pixels_per_chunk:
// shared edge pixels -> offset 0 and resolution - 1 pixels per chunk; separate cells -> 0.5 and resolution.

Texture2DArray<float> filter_source;
RWTexture2DArray<float> filter_destination;
StructuredBuffer<int3> filter_slot_coords; // per layer: chunk.xy, z = 1 when the layer is used
StructuredBuffer<int> filter_map;          // (chunk - filter_map_min) -> layer, -1 when not loaded
int2 filter_map_min;
int filter_map_size;
int filter_resolution;
float filter_chunk_size;   // meters
float filter_height_range; // meters represented by normalized height 1.0
int filter_pixels_per_chunk; // distance in pixels between the same pixel of two neighbouring chunks
float filter_pixel_offset;

// False for unused layers: the kernel should return without writing.
bool FilterTryGetChunk(uint layer, out int2 chunk)
{
    int3 slot = filter_slot_coords[layer];
    chunk = slot.xy;
    return slot.z != 0;
}

int FilterChunkToLayer(int2 chunk)
{
    int2 local = chunk - filter_map_min;
    if (any(local < 0) || any(local >= filter_map_size))
    {
        return -1;
    }
    return filter_map[local.x * filter_map_size + local.y];
}

// Source height of a pixel addressed relative to a chunk. Pixels outside the chunk
// (less than one chunk away) are read from the loaded neighbour, otherwise clamped to the edge.
float FilterLoadHeight(uint layer, int2 chunk, int2 pixel)
{
    int2 shift = int2(pixel.x < 0 ? -1 : (pixel.x >= filter_resolution ? 1 : 0),
                      pixel.y < 0 ? -1 : (pixel.y >= filter_resolution ? 1 : 0));
    if (shift.x != 0 || shift.y != 0)
    {
        int neighbour = FilterChunkToLayer(chunk + shift);
        if (neighbour >= 0)
        {
            // With shared edge pixels the neighbour starts one pixel earlier
            return filter_source[uint3(pixel - shift * filter_pixels_per_chunk, neighbour)];
        }
        pixel = clamp(pixel, 0, filter_resolution - 1);
    }
    return filter_source[uint3(pixel, layer)];
}

// Absolute position of a pixel in chunk units (1.0 = one chunk). Stable across loads,
// and equal for a shared edge pixel of two neighbouring chunks.
float2 FilterPixelToChunkSpace(int2 chunk, uint2 pixel)
{
    return chunk + (pixel + filter_pixel_offset) / filter_pixels_per_chunk;
}

#endif
