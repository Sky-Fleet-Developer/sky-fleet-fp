#ifndef TERRAIN_HEIGHT
#define TERRAIN_HEIGHT

// Height of the terrain: the heightmap, and the patch vertices built from it as the terrain vertex shader draws them.
// Shared by the terrain material (SampleHeightmap.hlsl) and compute shaders placing things on the terrain, which get
// the same values bound through Core.TerrainGenerator.ITerrainGpuData. Compute shaders declare the geomorphing keyword
// themselves: #pragma multi_compile_local _ TERRAIN_GEOMORPH

#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"
#include "TerrainLodMap.hlsl"

// Scene position of the terrain root: terrain heights are relative to it. Bound for compute shaders only
float4 terrain_origin;

void SampleHeightmap(float3 world_position, out float height)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);
    //world_position.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    //world_position.z = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    //height = _FadeTargetRange.x + fbm_perlin(world_position.xz * _ErosionUvScale, 3) * _FadeTargetRange.y;//
    float width, height_px, elements;
    source_heightmap.GetDimensions(width, height_px, elements);
    height = source_heightmap.SampleLevel(sampler_source_heightmap, float3(ChunkUvToTextureUv(uv, width, heightmap_shared_edges > 0.5), chunk), 0);
}

// Distance between two neighbouring heightmap pixels in world units
float HeightmapPixelSize()
{
    float width, height_px, elements;
    source_heightmap.GetDimensions(width, height_px, elements);
    float pixels_per_chunk = heightmap_shared_edges > 0.5 ? width - 1 : width;
    // position_to_chunk_matrix.x = 1 / heightmap chunk size in meters
    return 1.0 / (position_to_chunk_matrix.x * pixels_per_chunk);
}

// Normalized height and the normal of the heightmap at a point
void SampleHeightAndNormal(float3 world_position, out float height, out float3 normal)
{
    SampleHeightmap(world_position, height);

    // Central difference over one pixel on each side. Bilinear filtering makes the height linear inside a pixel,
    // so a smaller step returns the constant slope of a single pixel (faceted normals), while this step
    // returns the bilinear blend of the per-pixel slopes (smooth normals)
    float step = HeightmapPixelSize();
    float height_x_pos, height_z_pos, height_x_neg, height_z_neg;
    SampleHeightmap(world_position + float3(step, 0, 0), height_x_pos);
    SampleHeightmap(world_position + float3(0, 0, step), height_z_pos);

    SampleHeightmap(world_position + float3(-step, 0, 0), height_x_neg);
    SampleHeightmap(world_position + float3(0, 0, -step), height_z_neg);

    float dfdx = (height_x_pos - height_x_neg);
    float dfdz = (height_z_pos - height_z_neg);

    // Erosion and other heightmap filters are already baked into source_heightmap
    float2 gradient = float2(-dfdx, -dfdz);

    // Normalized height 1.0 equals height_scale meters
    float2 world_slope = gradient * (height_scale / (2.0 * step));
    normal = normalize(float3(world_slope.x, 1.0, world_slope.y));
}

// Height and normal of the vertex at index (see TerrainVertexIndex) of a patch of patch_size cells. With geomorphing
// they blend towards the ones the parent patch has there: the vertices off the parent grid lie on its straight edges,
// between two of its vertices. The triangles of Patch.CreateMesh are split along the (+1, -1) diagonal, so the
// vertices in the middle of a parent quad lie on that diagonal
void SamplePatchVertex(int2 index, float patch_size, out float height, out float3 normal)
{
    SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index)), height, normal);
#if defined(TERRAIN_GEOMORPH)
    float morph = TerrainMorph(TerrainVertexPosition(index), patch_size);
    int step = max((int)patch_size, 1); // between the vertices of the patch
    int2 odd = int2(FloorDiv(index.x, step), FloorDiv(index.y, step)) & 1;
    if (morph > 0 && any(odd))
    {
        int2 offset = odd.x && odd.y ? int2(step, -step) : (odd.x ? int2(step, 0) : int2(0, step));
        float height_a, height_b;
        float3 normal_a, normal_b;
        SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index + offset)), height_a, normal_a);
        SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index - offset)), height_b, normal_b);
        height = lerp(height, (height_a + height_b) * 0.5, morph);
        normal = normalize(lerp(normal, normalize(normal_a + normal_b), morph));
    }
#endif
}

// Height in meters above the terrain root of the patch mesh at a terrain space point: the triangles of the patch over
// its vertex heights, geomorphing included. The collapse on borders with coarser patches is left out, it moves the
// surface by less than a vertex step there
float TerrainMeshHeight(float2 terrain_xz)
{
    float patch_size = TerrainPatchSize(TerrainCell(terrain_xz));
    int step = max((int)patch_size, 1);
    float2 grid = terrain_xz / (TerrainVertexStep() * step);
    int2 quad = (int2)floor(grid);
    float2 f = grid - quad;
    int2 corner = quad * step;

    // The triangles of Patch.CreateMesh are split along the (+1, 0) - (0, +1) diagonal
    float3 unused_normal;
    float height_10, height_01, height_corner;
    SamplePatchVertex(corner + int2(step, 0), patch_size, height_10, unused_normal);
    SamplePatchVertex(corner + int2(0, step), patch_size, height_01, unused_normal);
    float height;
    if (f.x + f.y <= 1)
    {
        SamplePatchVertex(corner, patch_size, height_corner, unused_normal);
        height = height_corner + (height_10 - height_corner) * f.x + (height_01 - height_corner) * f.y;
    }
    else
    {
        SamplePatchVertex(corner + int2(step, step), patch_size, height_corner, unused_normal);
        height = height_corner + (height_01 - height_corner) * (1 - f.x) + (height_10 - height_corner) * (1 - f.y);
    }
    return height * height_scale;
}

#endif
