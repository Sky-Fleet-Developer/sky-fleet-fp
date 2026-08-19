#include "SampleHeightmapCBuffer.hlsl"

void SampleHeightmap_float(SamplerState heightmapSampler, float2 uv, out float height)
{
    int chunk_x = chunk_coord_x;
    int chunk_y = chunk_coord_y;
   
    uint2 slot = map[chunk_x * map_size + chunk_y];

    float2 pix = float2(pixel_size_uv_space, pixel_size_uv_space);
    float2 uv_to_sample = (slot + uv * pixel_size_uv_space * (heightmap_chunk_resolution - 2) + pix) * slots_count_inv;
    
    height = source_heightmap.SampleLevel(heightmapSampler, uv_to_sample, 0);
}

void SampleHeightmapOffset_float(SamplerState heightmapSampler, float2 uv, out float height)
{
    int chunk_x = chunk_coord_x;
    int chunk_y = chunk_coord_y;
    if (uv.x >= 1)
    {
        chunk_x++;
        uv.x = uv.x - 1;// + pixel_size_uv_space * 2;
    }
    else if (uv.x < 0)
    {
        chunk_x--;
        uv.x = uv.x + 1;// - pixel_size_uv_space * 2;
    }
    if (uv.y >= 1)
    {
        chunk_y++;
        uv.y = uv.y - 1;// + pixel_size_uv_space * 2;
    }
    else if (uv.y < 0)
    {
        chunk_y--;
        uv.y = uv.y + 1;// - pixel_size_uv_space * 2;
    }
    uint2 slot = map[chunk_x * map_size + chunk_y];
    //if (slot.x == -1)
    //{
    //    height = 0;
    //    return;
    //}

    //uv.x = 1 - uv.x;
    //uv.y = 1 - uv.y;
    float2 pix = float2(pixel_size_uv_space, pixel_size_uv_space);
    float2 uv_to_sample = (slot + uv * pixel_size_uv_space * (heightmap_chunk_resolution - 2) + pix) * slots_count_inv;
    
    height = source_heightmap.SampleLevel(heightmapSampler, uv_to_sample, 0);
}

