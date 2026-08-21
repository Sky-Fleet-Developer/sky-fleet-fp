#ifndef SAMPLE_MAP_UTILITY
#define SAMPLE_MAP_UTILITY
#include "MapBuffer.hlsl"

void WorldPositionToSlot(float3 world_position, out int2 chunk)
{
    world_position.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    world_position.z = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    chunk = floor(world_position.xz);
}

void WorldPositionToSlot(float3 world_position, out float2 chunk_space_uv, out int2 slot)
{
    world_position.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    world_position.z = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    slot = floor(world_position.xz);
    chunk_space_uv = frac(world_position.xz);
}

void WarpOffset(in out int2 chunk, in out float2 uv)
{
    if (uv.x >= 1)
    {
        chunk.x++;
        uv.x = uv.x - 1;// + pixel_size_uv_space * 2;
    }
    else if (uv.x < 0)
    {
        chunk.x--;
        uv.x = uv.x + 1;// - pixel_size_uv_space * 2;
    }
    if (uv.y >= 1)
    {
        chunk.y++;
        uv.y = uv.y - 1;// + pixel_size_uv_space * 2;
    }
    else if (uv.y < 0)
    {
        chunk.y--;
        uv.y = uv.y + 1;// - pixel_size_uv_space * 2;
    }
}

uint2 SlotToMap(int2 slot)
{
    return map[slot.x * map_size + slot.y];
}

float2 SlotUvToChunkUv(uint2 chunk, float2 uv)
{
    const float2 pix = float2(pixel_size_uv_space, pixel_size_uv_space);
    return (chunk + uv * pixel_size_uv_space * (heightmap_chunk_resolution - 2) + pix) * slots_count_inv;
}

#endif