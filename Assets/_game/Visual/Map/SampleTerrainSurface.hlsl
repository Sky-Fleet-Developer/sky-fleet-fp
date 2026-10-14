#ifndef SAMPLE_TERRAIN_SURFACE
#define SAMPLE_TERRAIN_SURFACE

#include "Alphamap.hlsl"
#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"
#include "TerrainView.hlsl"
#include "Assets/_game/Scripts/Core/TerrainGenerator/Shader/SurfaceAlphamap.hlsl"

// Surface types of the terrain, bound by TerrainProvider (Core.TerrainGenerator.Surfaces.TerrainSurfaces).
// Layer id of the texture arrays = surface id of the alphamap.
struct TerrainSurface
{
    float metallic;
    float smoothness;
    float tile_size; // meters covered by one repetition of the textures
};

StructuredBuffer<TerrainSurface> terrain_surfaces;
Texture2DArray terrain_surface_albedo;
Texture2DArray terrain_surface_normal; // normal maps as imported: RG or AG packed
Texture2DArray<float> terrain_surface_height;
SamplerState sampler_terrain_surface_albedo;
float terrain_surface_count;
float terrain_surface_blend_depth; // height-based blending: height difference over which surfaces mix
float2 terrain_map_origin; // terrain space meters of the map corner, so textures stay put when the world origin moves

// Surfaces sampled per pixel, the strongest of the alphamap texels around it
#define TERRAIN_SURFACE_SAMPLES 4

float3 DecodeTerrainSurfaceNormal(float4 packed)
{
    // RG (BC5) and AG (DXT5nm) packing: for RG the alpha is 1, for AG the red is 1
    packed.a *= packed.r;
    float2 xy = packed.ag * 2 - 1;
    return float3(xy, sqrt(saturate(1 - dot(xy, xy))));
}

// Terrain surface of a pixel. Surfaces are blended by their height maps: within terrain_surface_blend_depth of the
// highest (height + weight), the higher surface wins. The surface normal is planar mapped on the terrain (u = +x,
// v = +z) and returned in the tangent space of the given basis. In the other view modes debug_color is returned.
// world_position: absolute world position, as passed to SampleHeightmapWithGradient.
void SampleTerrainSurface_float(float3 world_position, float3 normal_ws, float3 tangent_ws, float3 bitangent_ws,
    float3 debug_color, out float3 base_color, out float3 normal_ts, out float metallic, out float smoothness)
{
    // Derivatives before any branch
    float2 terrain_xz = world_position.xz - position_to_chunk_matrix.zw + terrain_map_origin;
    float2 terrain_ddx = ddx(terrain_xz);
    float2 terrain_ddy = ddy(terrain_xz);

    base_color = debug_color;
    normal_ts = float3(0, 0, 1);
    metallic = 0;
    smoothness = 0.3;
    if (TerrainViewMode() != 0 || terrain_surface_count < 1)
    {
        return;
    }

    // The encoded alphamap can't be filtered: decode the four texels around the pixel and weigh them bilinearly
    int slot;
    float2 chunk_uv;
    WorldPositionToSlot(world_position, chunk_uv, slot);
    int layer = SlotToMap(slot);
    float width, height, elements;
    source_alphamap.GetDimensions(width, height, elements);
    float2 texel = heightmap_shared_edges > 0.5 ? chunk_uv * (width - 1) : chunk_uv * width - 0.5;
    float2 cell = floor(texel);
    float2 f = texel - cell;
    int2 i0 = clamp((int2)cell, 0, (int)width - 1);
    int2 i1 = clamp((int2)cell + 1, 0, (int)width - 1);
    SurfacesBegin();
    SurfacesAddEncoded(source_alphamap.Load(int4(i0.x, i0.y, layer, 0)), (1 - f.x) * (1 - f.y));
    SurfacesAddEncoded(source_alphamap.Load(int4(i1.x, i0.y, layer, 0)), f.x * (1 - f.y));
    SurfacesAddEncoded(source_alphamap.Load(int4(i0.x, i1.y, layer, 0)), (1 - f.x) * f.y);
    SurfacesAddEncoded(source_alphamap.Load(int4(i1.x, i1.y, layer, 0)), f.x * f.y);
    SurfacesSortStrongest(TERRAIN_SURFACE_SAMPLES);
    int samples = min(surface_candidate_count, TERRAIN_SURFACE_SAMPLES);
    if (samples == 0)
    {
        return;
    }

    float total_weight = 0;
    [unroll]
    for (int w = 0; w < TERRAIN_SURFACE_SAMPLES; w++)
    {
        total_weight += w < samples ? surface_candidate_weight[w] : 0;
    }

    float score[TERRAIN_SURFACE_SAMPLES];
    float3 albedo[TERRAIN_SURFACE_SAMPLES];
    float3 detail[TERRAIN_SURFACE_SAMPLES];
    float2 surface[TERRAIN_SURFACE_SAMPLES]; // metallic, smoothness
    float best = -3.0e38;
    [unroll]
    for (int k = 0; k < TERRAIN_SURFACE_SAMPLES; k++)
    {
        score[k] = -3.0e38;
        albedo[k] = 0;
        detail[k] = float3(0, 0, 1);
        surface[k] = 0;
        if (k < samples)
        {
            int id = surface_candidate_id[k];
            TerrainSurface parameters = terrain_surfaces[id];
            float scale = 1 / max(parameters.tile_size, 1e-3);
            float3 uvw = float3(terrain_xz * scale, id);
            float2 gx = terrain_ddx * scale;
            float2 gy = terrain_ddy * scale;
            albedo[k] = terrain_surface_albedo.SampleGrad(sampler_terrain_surface_albedo, uvw, gx, gy).rgb;
            detail[k] = DecodeTerrainSurfaceNormal(terrain_surface_normal.SampleGrad(sampler_terrain_surface_albedo, uvw, gx, gy));
            float surface_height = terrain_surface_height.SampleGrad(sampler_terrain_surface_albedo, uvw, gx, gy);
            surface[k] = float2(parameters.metallic, parameters.smoothness);
            score[k] = surface_candidate_weight[k] / total_weight + surface_height;
            best = max(best, score[k]);
        }
    }

    float threshold = best - max(terrain_surface_blend_depth, 1e-4);
    float blend_total = 0;
    float3 color = 0;
    float3 detail_normal = 0;
    float2 surface_sum = 0;
    [unroll]
    for (int b = 0; b < TERRAIN_SURFACE_SAMPLES; b++)
    {
        float blend = max(score[b] - threshold, 0);
        blend_total += blend;
        color += albedo[b] * blend;
        detail_normal += detail[b] * blend;
        surface_sum += surface[b] * blend;
    }

    base_color = color / blend_total;
    metallic = surface_sum.x / blend_total;
    smoothness = surface_sum.y / blend_total;

    // Planar mapped detail normal on the terrain normal, then into the given tangent space
    float3 n = normalize(normal_ws);
    float3 axis_u = normalize(float3(1, 0, 0) - n * n.x);
    float3 axis_v = cross(axis_u, n);
    float3 d = normalize(detail_normal);
    float3 world_normal = normalize(d.x * axis_u + d.y * axis_v + d.z * n);
    normal_ts = normalize(float3(dot(world_normal, tangent_ws), dot(world_normal, bitangent_ws), dot(world_normal, n)));
}

#endif
