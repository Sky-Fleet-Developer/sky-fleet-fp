#pragma once
#include "Alphamap.hlsl"
#include "SampleMapUtility.hlsl"

void SampleAlphamap_float(float3 world_position, out float4 color)
{
    int2 slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    uint2 chunk = SlotToMap(slot);

    float2 uv_to_sample = SlotUvToChunkUv(chunk, uv);
    
    color = source_alphamap.SampleLevel(sampler_source_alphamap, uv_to_sample, 0);
}