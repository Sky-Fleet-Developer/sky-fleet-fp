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
    float2 height_remap; // heights of height texture values 0 and 1
};

StructuredBuffer<TerrainSurface> terrain_surfaces;
Texture2DArray terrain_surface_albedo;
Texture2DArray terrain_surface_normal; // normal maps as imported: RG or AG packed
Texture2DArray<float> terrain_surface_height;
// Inline sampler, defined by its name: each pass may use only some of the arrays (e.g. depth passes skip the albedo),
// so a sampler named after one of them would lose its texture where that one is stripped
SamplerState terrain_surface_trilinear_repeat_aniso8_sampler;
float terrain_surface_count;
float terrain_surface_blend_depth; // height-based blending: height difference over which surfaces mix
float2 terrain_map_origin; // terrain space meters of the map corner, so textures stay put when the world origin moves
// Stochastic tiling (Heitz & Neyret 2018, Mikkelsen 2022 "hex-tiling"): every texture repetition is offset and rotated
// at random over a triangle grid, and the three tiles around a pixel are blended, so the tiling doesn't show
float terrain_surface_stochastic;          // 1 on, 0 plain tiling
float terrain_surface_stochastic_contrast; // 0..1: how much the height maps decide the blend between tiles
float terrain_surface_stochastic_rotation; // 0..1: largest random rotation of a tile, in turns

// Surfaces sampled per pixel, the strongest of the alphamap texels around it
#define TERRAIN_SURFACE_SAMPLES 4

float3 DecodeTerrainSurfaceNormal(float4 packed)
{
    // RG (BC5) and AG (DXT5nm) packing: for RG the alpha is 1, for AG the red is 1
    packed.a *= packed.r;
    float2 xy = packed.ag * 2 - 1;
    return float3(xy, sqrt(saturate(1 - dot(xy, xy))));
}

uint TerrainSurfaceHash(int2 cell, uint salt)
{
    // pcg2d
    uint2 v = asuint(cell) * 1664525u + 1013904223u + salt * 2654435769u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    return v.x ^ v.y;
}

float2 RotateTerrainVector(float2 v, float2 cos_sin)
{
    return float2(v.x * cos_sin.x - v.y * cos_sin.y, v.x * cos_sin.y + v.y * cos_sin.x);
}

// One repetition of a surface: uv rotated and offset. The normal comes back in uv space (rotated back)
void SampleTerrainSurfaceTile(int id, float2 uv, float2 gx, float2 gy, float2 cos_sin, float2 offset,
    out float3 albedo, out float3 normal, out float height)
{
    float3 uvw = float3(RotateTerrainVector(uv, cos_sin) + offset, id);
    float2 tile_gx = RotateTerrainVector(gx, cos_sin);
    float2 tile_gy = RotateTerrainVector(gy, cos_sin);
    albedo = terrain_surface_albedo.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy).rgb;
    normal = DecodeTerrainSurfaceNormal(terrain_surface_normal.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy));
    normal.xy = RotateTerrainVector(normal.xy, float2(cos_sin.x, -cos_sin.y));
    height = terrain_surface_height.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy);
}

// Random rotation and offset of the tile at a triangle grid vertex
void TerrainSurfaceTileTransform(int2 vertex, int id, out float2 cos_sin, out float2 offset)
{
    uint hash = TerrainSurfaceHash(vertex, (uint)id);
    offset = float2(hash & 0xFFFF, hash >> 16) / 65536.0;
    float angle = TerrainSurfaceHash(vertex, (uint)id + 101u) * (6.28318530718 / 4294967296.0) * terrain_surface_stochastic_rotation;
    cos_sin = float2(cos(angle), sin(angle));
}

// Stochastic tiling of a surface: the three tiles of the triangle grid around uv, blended
void SampleTerrainSurfaceStochastic(int id, float2 uv, float2 gx, float2 gy, out float3 albedo, out float3 normal, out float height)
{
    // Triangle grid: cells of about a third of a texture repetition (2 * sqrt(3) per repetition)
    float2 grid = uv * 3.46410162;
    float2 skewed = float2(grid.x - 0.57735027 * grid.y, 1.15470054 * grid.y);
    int2 base = (int2)floor(skewed);
    float2 f = skewed - base;
    float rest = 1 - f.x - f.y;
    int2 v0 = rest > 0 ? base : base + int2(1, 1);
    int2 v1 = base + (rest > 0 ? int2(0, 1) : int2(1, 0));
    int2 v2 = base + (rest > 0 ? int2(1, 0) : int2(0, 1));
    float3 weights = rest > 0 ? float3(rest, f.y, f.x) : float3(-rest, 1 - f.y, 1 - f.x);

    float3 albedos[3];
    float3 normals[3];
    float3 heights;
    int2 vertices[3] = { v0, v1, v2 };
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float2 cos_sin, offset;
        TerrainSurfaceTileTransform(vertices[i], id, cos_sin, offset);
        float tile_height;
        SampleTerrainSurfaceTile(id, uv, gx, gy, cos_sin, offset, albedos[i], normals[i], tile_height);
        heights[i] = tile_height;
    }

    // Higher parts of a tile cover lower parts of its neighbours, then a sharp falloff keeps the tiles crisp
    weights *= lerp(1, heights + 0.05, terrain_surface_stochastic_contrast);
    weights = pow(max(weights, 0), 7);
    weights /= max(weights.x + weights.y + weights.z, 1e-6);

    albedo = albedos[0] * weights.x + albedos[1] * weights.y + albedos[2] * weights.z;
    normal = normalize(normals[0] * weights.x + normals[1] * weights.y + normals[2] * weights.z);
    height = dot(heights, weights);
}

// A surface at uv (in repetitions of its textures): plain, or stochastic over a triangle grid
void SampleTerrainSurfaceTextures(int id, float2 uv, float2 gx, float2 gy, out float3 albedo, out float3 normal, out float height)
{
    if (terrain_surface_stochastic > 0.5)
    {
        SampleTerrainSurfaceStochastic(id, uv, gx, gy, albedo, normal, height);
    }
    else
    {
        SampleTerrainSurfaceTile(id, uv, gx, gy, float2(1, 0), 0, albedo, normal, height);
    }
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
            float surface_height;
            SampleTerrainSurfaceTextures(id, terrain_xz * scale, terrain_ddx * scale, terrain_ddy * scale,
                albedo[k], detail[k], surface_height);
            surface_height = lerp(parameters.height_remap.x, parameters.height_remap.y, surface_height);
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
