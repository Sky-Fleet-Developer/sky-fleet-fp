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

int FloorDiv(int a, int b) // b > 0
{
    return (a < 0 ? a - b + 1 : a) / b;
}

// The vertex grid of the patches of one cell, the finest one. The vertices of every patch lie on it, so a vertex is
// addressed by its integer index there: the same whichever patch draws it, free of the rounding of each patch transform.
// One unit = width_scale / terrain_patch_resolution meters; the vertices of a patch of S cells are S units apart
float TerrainVertexStep()
{
    return width_scale / terrain_patch_resolution;
}

int2 TerrainVertexIndex(float2 terrain_xz)
{
    return (int2)round(terrain_xz / TerrainVertexStep());
}

float2 TerrainVertexPosition(int2 index)
{
    return index * TerrainVertexStep();
}

// The patch whose vertex grid a vertex follows (its own, or the coarser one on a border between patches of different
// sizes) and the vertex it becomes. On such a border the vertices of the finer side collapse onto the nearest vertex
// of the coarser side, so both sides end in exactly the same vertices: a vertex merely lying on the coarse edge would
// be a T-junction, which leaves pixel holes after rasterization however exact the math is. The vertices of the
// coarser side lie on its own grid and stay. Cell corners stay too, they lie on the grid of every patch while the
// resolution is a multiple of the patch sizes
void TerrainStitch(int2 index, out float patch_size, out int2 target)
{
    target = index;
    int resolution = (int)terrain_patch_resolution;
    int2 cell = int2(FloorDiv(index.x, resolution), FloorDiv(index.y, resolution));
    patch_size = TerrainPatchSize(cell);

    bool2 on_border = index == cell * resolution;
    if (on_border.x == on_border.y)
    {
        return;
    }

    // x: across the border, y: along it
    int2 local = on_border.x ? index : index.yx;
    int2 local_cell = on_border.x ? cell : cell.yx;
    float size_before = TerrainPatchSize(on_border.x ? int2(local_cell.x - 1, local_cell.y) : int2(local_cell.y, local_cell.x - 1));
    float size_after = TerrainPatchSize(cell);
    patch_size = max(size_before, size_after);
    // Same LOD on both sides, or the edge of the loaded patches
    if (size_before == size_after || min(size_before, size_after) < 0.5)
    {
        return;
    }

    int step = (int)patch_size; // between the coarse vertices
    int below = FloorDiv(local.y, step) * step;
    int along = (local.y - below) * 2 <= step ? below : below + step;
    target = on_border.x ? int2(local.x, along) : int2(along, local.x);
}

// Geomorphing (TERRAIN_GEOMORPH): approaching the distance where a patch merges into its parent, its vertices blend to
// the heights the parent's coarser grid gives them, so the LOD switch changes nothing on screen.
// Bound by TerrainProvider from QuadTreeLodConfig
float terrain_morph_end[8]; // by log2 of the patch size: view distance where the patch merges into its parent
float terrain_morph_range; // part of that distance over which the vertices blend
float2 terrain_lod_view_position; // terrain space xz the quadtree is built around

// 0: the heights of the patch itself, 1: the heights of its parent
float TerrainMorph(float2 terrain_xz, float patch_size)
{
    if (patch_size < 0.5)
    {
        return 0;
    }

    float end = terrain_morph_end[clamp((int)round(log2(patch_size)), 0, 7)];
    float start = end * (1 - terrain_morph_range);
    return saturate((distance(terrain_xz, terrain_lod_view_position) - start) / max(end - start, 1e-3));
}

#endif
