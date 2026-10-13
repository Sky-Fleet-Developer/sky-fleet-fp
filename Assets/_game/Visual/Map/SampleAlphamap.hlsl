#pragma once
#include "Alphamap.hlsl"
#include "SampleMapUtility.hlsl"

void SampleAlphamap_float(float3 world_position, out float4 color)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);

    float width, height, elements;
    source_alphamap.GetDimensions(width, height, elements);
    color = source_alphamap.SampleLevel(sampler_source_alphamap, float3(ChunkUvToTextureUv(uv, width, true), chunk), 0);
}