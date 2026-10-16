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

#endif
