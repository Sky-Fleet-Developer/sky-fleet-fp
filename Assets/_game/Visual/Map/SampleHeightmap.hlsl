#include "SampleHeightmapCBuffer.hlsl"

void SampleHeightmap_float(SamplerState heightmapSampler, float2 uv, out float height)
{
    float x = uv.x;
    uv.x = uv.y;
    uv.y = x;
    int chunk_x = chunk_coord_x;
    int chunk_y = chunk_coord_y;
    if (uv.x > 0.99f)
    {
        chunk_y++;
        uv.x -= 1;
    }
    if (uv.y > 0.99f)
    {
        chunk_x++;
        uv.y -= 1;
    }
    if (uv.x < 0.01)
    {
        chunk_y--;
        uv.x += 1;
    }
    if (uv.y < 0.01)
    {
        chunk_x--;
        uv.y += 1;
    }
    uint2 slot = map[chunk_x * map_size + chunk_y];
    if (slot.x == -1)
    {
        height = 0;
        return;
    }

    //uv.x = 1 - uv.x;
    //uv.y = 1 - uv.y;
    float2 uv_to_sample = (float2(slot.x, slot.y) + uv) * slots_count_inv;

    height = source_heightmap.SampleLevel(heightmapSampler, uv_to_sample, 0);
}

