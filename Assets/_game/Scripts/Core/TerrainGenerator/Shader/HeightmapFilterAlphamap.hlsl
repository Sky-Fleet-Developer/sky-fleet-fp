#ifndef HEIGHTMAP_FILTER_ALPHAMAP
#define HEIGHTMAP_FILTER_ALPHAMAP

#include "HeightmapFilterCommon.hlsl"

// Alphamap (surface weights, RGBA) for heightmap filter kernels. Bound by HeightmapFilterContext.BindAlphamap.
// Same layers and pixel layout as the heights. A filter that writes filter_alpha_destination must report
// IHeightmapFilter.WritesAlphamap and write every pixel of every used layer, like the heights.

Texture2DArray<float4> filter_alpha_source;
RWTexture2DArray<float4> filter_alpha_destination;

// Source alphamap of a pixel addressed relative to a chunk, read across chunk edges like FilterLoadHeight
float4 FilterLoadAlpha(uint layer, int2 chunk, int2 pixel)
{
    int2 shift = int2(pixel.x < 0 ? -1 : (pixel.x >= filter_resolution ? 1 : 0),
                      pixel.y < 0 ? -1 : (pixel.y >= filter_resolution ? 1 : 0));
    uint3 texel = uint3(pixel, layer);
    if (shift.x != 0 || shift.y != 0)
    {
        int neighbour = FilterChunkToLayer(chunk + shift);
        // With shared edge pixels the neighbour starts one pixel earlier
        texel = neighbour >= 0
            ? uint3(pixel - shift * filter_pixels_per_chunk, neighbour)
            : uint3(clamp(pixel, 0, filter_resolution - 1), layer);
    }
    return filter_alpha_source[texel];
}

#endif
