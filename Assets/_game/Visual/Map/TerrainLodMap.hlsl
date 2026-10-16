#ifndef TERRAIN_LOD_MAP
#define TERRAIN_LOD_MAP

#include "MapBuffer.hlsl"

// Quadtree leaves, one texel per cell of width_scale meters: the size in cells of the patch covering the cell,
// 0 where there is none. Bound by TerrainProvider (Core.TerrainGenerator.LOD.PatchLodMap)
Texture2D<float> terrain_lod_map;
float4 terrain_lod_map_rect; // xy first cell, zw size in cells
float terrain_patch_resolution; // quads along a patch side, whatever its size

int2 TerrainCell(float2 terrain_xz)
{
    return (int2)floor(terrain_xz / width_scale);
}

// Size in cells of the patch covering the cell, 0 outside of the patches
float TerrainPatchSize(int2 cell)
{
    int2 texel = cell - (int2)terrain_lod_map_rect.xy;
    if (any(texel < 0) || any(texel >= (int2)terrain_lod_map_rect.zw))
    {
        return 0;
    }
    return terrain_lod_map.Load(int3(texel, 0));
}

// A vertex on the border between patches of different sizes must lie on the straight segment the coarser patch draws
// between its own two vertices there, or the border cracks. Returns false when the vertex needs nothing; otherwise
// the terrain space offsets from the vertex to those two coarse vertices and the blend between them.
// Only the finer side moves: the vertices of the coarser one lie on its own grid, where the blend is 0.
// Cell corners are skipped, they lie on the grid of every patch while the resolution is a multiple of the patch sizes
bool TerrainStitch(float2 terrain_xz, out float2 offset_a, out float2 offset_b, out float t)
{
    offset_a = 0;
    offset_b = 0;
    t = 0;

    float2 cells = terrain_xz / width_scale;
    // Closer than a quarter of the finest vertex step to a cell border
    bool2 on_border = abs(cells - round(cells)) * terrain_patch_resolution < 0.25;
    if (on_border.x == on_border.y)
    {
        return false;
    }

    // x: across the border, y: along it
    float2 local = on_border.x ? cells : cells.yx;
    int border = (int)round(local.x);
    int row = (int)floor(local.y);
    float size_before = TerrainPatchSize(on_border.x ? int2(border - 1, row) : int2(row, border - 1));
    float size_after = TerrainPatchSize(on_border.x ? int2(border, row) : int2(row, border));
    // Same LOD on both sides, or the edge of the loaded patches
    if (size_before == size_after || min(size_before, size_after) < 0.5)
    {
        return false;
    }

    float step = max(size_before, size_after) / terrain_patch_resolution; // cells between the coarse vertices
    float k = local.y / step;
    t = frac(k);
    if (t < 1e-3 || t > 1 - 1e-3)
    {
        return false;
    }

    float2 along = on_border.x ? float2(0, 1) : float2(1, 0);
    offset_a = along * (-t * step * width_scale);
    offset_b = along * ((1 - t) * step * width_scale);
    return true;
}

// Color of the LOD view mode: one color per patch size, patch borders darkened. Pixel shader only (uses fwidth)
float3 TerrainLodViewColor(float2 terrain_xz)
{
    static const float3 palette[5] =
    {
        float3(0.2, 0.75, 0.3),  // 1 cell
        float3(0.95, 0.85, 0.2), // 2
        float3(0.95, 0.5, 0.15), // 4
        float3(0.85, 0.2, 0.2),  // 8
        float3(0.55, 0.25, 0.8), // 16 and more
    };

    float2 cells = terrain_xz / width_scale;
    float size = TerrainPatchSize((int2)floor(cells));
    if (size < 0.5)
    {
        return float3(0.1, 0.1, 0.1);
    }

    float3 color = palette[clamp((int)round(log2(size)), 0, 4)];
    // Patches are aligned to their own size, so the position inside the patch is a fraction
    float2 patch_uv = cells / size;
    float2 inside = frac(patch_uv);
    float2 border_pixels = min(inside, 1 - inside) / max(fwidth(patch_uv), 1e-6);
    float border = saturate(min(border_pixels.x, border_pixels.y) - 0.5);
    return color * lerp(0.15, 1, border);
}

#endif
