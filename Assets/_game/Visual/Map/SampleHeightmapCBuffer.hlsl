#ifndef SAMPLE_HEIGHTMAP_CBUFFER
#define SAMPLE_HEIGHTMAP_CBUFFER

Texture2D<float> source_heightmap;
SamplerState sampler_source_heightmap;
StructuredBuffer<uint2> map;
int chunk_coord_x;
int chunk_coord_y;
int map_size; 
float slots_count_inv;
float height_scale;

#endif