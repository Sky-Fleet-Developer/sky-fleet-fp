#pragma once
#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"


void SampleHeightmap(float3 world_position, out float height)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);

    height = source_heightmap.SampleLevel(sampler_source_heightmap, float3(uv, chunk), 0) * height_scale;
}

void SampleHeightmapWithGradient_float(float3 world_position, out float height, out float2 gradient)
{
    SampleHeightmap(world_position, height);
    
    float height_x, height_z;
    SampleHeightmap(world_position + float3(1, 0, 0), height_x);
    SampleHeightmap(world_position + float3(0, 0, 1), height_z);

    float dfdx = (height_x - height);
    float dfdz = (height_z - height);
    gradient = float2(-dfdx, -dfdz);
}