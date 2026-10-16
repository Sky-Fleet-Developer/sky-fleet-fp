#ifndef TERRAIN_SURFACE_WEIGHTS
#define TERRAIN_SURFACE_WEIGHTS

// Surface types of the terrain at a point, from the encoded alphamap. Shared by the terrain material and compute shaders
// (bound through Core.TerrainGenerator.ITerrainGpuData)

#include "Alphamap.hlsl"
#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"
#include "Assets/_game/Scripts/Core/TerrainGenerator/Shader/SurfaceAlphamap.hlsl"

// Collects the surfaces around an absolute world position into the candidate list of SurfaceAlphamap.hlsl
// (SurfacesBegin). The encoded alphamap can't be filtered: the four texels around the point are decoded and weighed
// bilinearly
void CollectTerrainSurfaces(float3 world_position)
{
    int slot;
    float2 chunk_uv;
    WorldPositionToSlot(world_position, chunk_uv, slot);
    int layer = SlotToMap(slot);
    float width, height, elements;
    source_alphamap.GetDimensions(width, height, elements);
    float2 texel = heightmap_shared_edges > 0.5 ? chunk_uv * (width - 1) : chunk_uv * width - 0.5;
    float2 cell = floor(texel);
    float2 f = texel - cell;
    int2 i0 = clamp((int2)cell, 0, (int)width - 1);
    int2 i1 = clamp((int2)cell + 1, 0, (int)width - 1);
    SurfacesBegin();
    SurfacesAddEncoded(source_alphamap.Load(int4(i0.x, i0.y, layer, 0)), (1 - f.x) * (1 - f.y));
    SurfacesAddEncoded(source_alphamap.Load(int4(i1.x, i0.y, layer, 0)), f.x * (1 - f.y));
    SurfacesAddEncoded(source_alphamap.Load(int4(i0.x, i1.y, layer, 0)), (1 - f.x) * f.y);
    SurfacesAddEncoded(source_alphamap.Load(int4(i1.x, i1.y, layer, 0)), f.x * f.y);
}

#endif
