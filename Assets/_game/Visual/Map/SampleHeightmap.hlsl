#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"
#include "Assets/_game/Scripts/Core/TerrainGenerator/Shader/ErodeUsingSettings.hlsl"

float2 hash_p(float2 p) { 
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))); 
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123); 
}

// Базовый 2D шум Перлина
float perlin_noise(float2 uv) { 
    float2 i = floor(uv); 
    float2 f = frac(uv); 

    // Сглаживание по кривой Эрмита (Quintic interpolation) 
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); 

    // Градиенты в четырех углах ячейки (корректные смещения)
    float dotBottomLeft  = dot(hash_p(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)); 
    float dotBottomRight = dot(hash_p(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)); 
    float dotTopLeft     = dot(hash_p(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)); 
    float dotTopRight    = dot(hash_p(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)); 

    // Корректная интерполяция: сначала по X (низ и верх), затем по Y между ними
    return lerp( 
        lerp(dotBottomLeft, dotBottomRight, u.x), 
        lerp(dotTopLeft, dotTopRight, u.x), 
        u.y 
    ); 
}

// Генерация шума с заданным количеством октав (FBM)
float fbm_perlin(float2 uv, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = 1.0;
    float maxValue = 0.0; // Используется для нормализации результата в диапазон [-1, 1] или [0, 1]

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * perlin_noise(uv * frequency);
        maxValue += amplitude;
        frequency *= lacunarity;
        amplitude *= gain;
    }

    // Возвращает шум в диапазоне [0.0, 1.0]
    return (value / maxValue) * 0.5 + 0.5;
}

void SampleHeightmap(float3 world_position, out float height)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);
    //world_position.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    //world_position.z = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    //height = _FadeTargetRange.x + fbm_perlin(world_position.xz * _ErosionUvScale, 3) * _FadeTargetRange.y;//
    height = source_heightmap.SampleLevel(sampler_source_heightmap, float3(uv, chunk), 0);
}

void SampleHeightmapWithGradient_float(float3 world_position, out float height, out float3 normal, out float3 debug_color)
{
    SampleHeightmap(world_position, height);

    float height_x_pos, height_z_pos, height_x_neg, height_z_neg;
    SampleHeightmap(world_position + float3(1, 0, 0), height_x_pos);
    SampleHeightmap(world_position + float3(0, 0, 1), height_z_pos);

    SampleHeightmap(world_position + float3(-1, 0, 0), height_x_neg);
    SampleHeightmap(world_position + float3(0, 0, -1), height_z_neg);

    float dfdx = (height_x_pos - height_x_neg);
    float dfdz = (height_z_pos - height_z_neg);

    float2 gradient = float2(-dfdx, -dfdz);

    float2 non_clamped_uv;
    non_clamped_uv.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    non_clamped_uv.y = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    debug_color = float3(1, 1, 1);

    if (erosion_strength != 0 && erosion_scale != 0)
    {
        float debug;
        float ridgeMap;
        float4 erosion = ErodeUsingSettings(non_clamped_uv, float3(height, gradient), height - 0.5, debug, ridgeMap);
        gradient += erosion.yz / erosion_gully_weight * _ErosionGradientInfluence;
        height += erosion.x;
        debug_color = float3(debug, .5f, .5f);
    }

    float2 world_slope = gradient * (height_scale / 2.0);
    normal = normalize(float3(world_slope.x, 1.0, world_slope.y));

    height *= height_scale * 4;
}