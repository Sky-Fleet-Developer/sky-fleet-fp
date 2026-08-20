#include "SampleHeightmapCBuffer.hlsl"

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

void SampleHeightmapOffset_float(SamplerState heightmapSampler, float3 world_position, out float height)
{
    int2 slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    //WarpOffset(slot, uv);
    uint2 chunk = map[slot.x * map_size + slot.y];

    float2 pix = float2(pixel_size_uv_space, pixel_size_uv_space);
    float2 uv_to_sample = (chunk + uv * pixel_size_uv_space * (heightmap_chunk_resolution - 2) + pix) * slots_count_inv;
    
    height = source_heightmap.SampleLevel(heightmapSampler, uv_to_sample, 0) * height_scale;
}

void SampleHeightmap_float(SamplerState heightmapSampler, float3 world_position, out float height, out float2 gradient)
{
    int2 slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    uint2 chunk = map[slot.x * map_size + slot.y];

    const float2 pix = float2(pixel_size_uv_space, pixel_size_uv_space);
    float2 uv_to_sample = (chunk + uv * pixel_size_uv_space * (heightmap_chunk_resolution - 2) + pix) * slots_count_inv;
    
    height = source_heightmap.SampleLevel(heightmapSampler, uv_to_sample, 0) * height_scale;

    
    float height_x, height_z;
    SampleHeightmapOffset_float(sampler_source_heightmap, world_position + float3(position_to_chunk_matrix.x, 0, 0), height_x);
    SampleHeightmapOffset_float(sampler_source_heightmap, world_position + float3(0, 0, position_to_chunk_matrix.y), height_z);

    float dfdx = (height_x - height) * heightmap_chunk_resolution / width_scale;
    float dfdz = (height_z - height) * heightmap_chunk_resolution / width_scale;
    gradient = float2(-dfdx, -dfdz);
}