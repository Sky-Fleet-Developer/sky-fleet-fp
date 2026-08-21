#ifndef MAP_BUFFER
#define MAP_BUFFER

StructuredBuffer<uint2> map;
int map_size; 
float pixel_size_uv_space;
float heightmap_chunk_resolution;
float slots_count_inv;
float height_scale;
float width_scale;
float4 position_to_chunk_matrix;

#endif