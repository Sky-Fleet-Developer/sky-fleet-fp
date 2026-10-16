#ifndef MAP_BUFFER
#define MAP_BUFFER

StructuredBuffer<int> map;
int map_size; 
float pixel_size_uv_space;
float slots_count_inv;
float height_scale;
float width_scale;
float4 position_to_chunk_matrix;
float2 terrain_map_origin; // terrain space meters of the map corner, so textures stay put when the world origin moves

// Terrain space xz (the space of the patch rects and chunk coordinates) of an absolute world position
float2 TerrainSpacePosition(float3 world_position)
{
    return world_position.xz - position_to_chunk_matrix.zw + terrain_map_origin;
}

// Absolute world position (height 0) of a terrain space point, the inverse of TerrainSpacePosition
float3 TerrainToWorldPosition(float2 terrain_xz)
{
    float2 xz = terrain_xz + position_to_chunk_matrix.zw - terrain_map_origin;
    return float3(xz.x, 0, xz.y);
}

#endif
