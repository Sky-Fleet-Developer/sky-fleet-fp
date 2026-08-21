#pragma once
#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"


void SampleHeightmapOffset_float(float3 world_position, out float height)
{
    int2 slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    uint2 chunk = SlotToMap(slot);

    float2 uv_to_sample = SlotUvToChunkUv(chunk, uv);
    
    height = source_heightmap.SampleLevel(sampler_source_heightmap, uv_to_sample, 0) * height_scale;
}

void SampleHeightmap_float(float3 world_position, out float height, out float2 gradient)
{
    int2 slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    uint2 chunk = SlotToMap(slot);

    float2 uv_to_sample = SlotUvToChunkUv(chunk, uv);
    
    height = source_heightmap.SampleLevel(sampler_source_heightmap, uv_to_sample, 0) * height_scale;

    
    float height_x, height_z;
    SampleHeightmapOffset_float(world_position + float3(position_to_chunk_matrix.x, 0, 0), height_x);
    SampleHeightmapOffset_float(world_position + float3(0, 0, position_to_chunk_matrix.y), height_z);

    float dfdx = (height_x - height) * heightmap_chunk_resolution / width_scale;
    float dfdz = (height_z - height) * heightmap_chunk_resolution / width_scale;
    gradient = float2(-dfdx, -dfdz);
}