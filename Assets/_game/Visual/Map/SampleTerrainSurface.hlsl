#ifndef SAMPLE_TERRAIN_SURFACE
#define SAMPLE_TERRAIN_SURFACE

#include "TerrainSurfaceWeights.hlsl"
#include "TerrainView.hlsl"
#include "TerrainLodMap.hlsl"
#include "Assets/_game/Visual/Common/StochasticTiling.hlsl"

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
// Stochastic tiling (StochasticTiling.hlsl): texture repetitions offset and rotated at random, so the tiling doesn't show
float terrain_surface_stochastic;          // 1 on, 0 plain tiling
float terrain_surface_stochastic_contrast; // 0..1: how much the height maps decide the blend between tiles
float terrain_surface_stochastic_rotation; // 0..1: largest random rotation of a tile, in turns

// Far tint (Core.TerrainGenerator.ITerrainFarTint): detail layers that fade out with distance (grass) hand the terrain
// their color, which the surfaces blend towards past the end of their range
StructuredBuffer<float4> terrain_far_tint; // by surface id: rgb color, a amount
float terrain_far_tint_count; // entries of terrain_far_tint, 0 when there's no tint
float3 terrain_far_tint_center; // absolute world position the distance is measured from
float2 terrain_far_tint_range; // distances where the tint starts and is complete

// Surfaces sampled per pixel, the strongest of the alphamap texels around it
#define TERRAIN_SURFACE_SAMPLES 4

float3 DecodeTerrainSurfaceNormal(float4 packed)
{
    // RG (BC5) and AG (DXT5nm) packing: for RG the alpha is 1, for AG the red is 1
    packed.a *= packed.r;
    float2 xy = packed.ag * 2 - 1;
    return float3(xy, sqrt(saturate(1 - dot(xy, xy))));
}

// One repetition of a surface: uv rotated and offset. The normal comes back in uv space (rotated back)
void SampleTerrainSurfaceTile(int id, float2 uv, float2 gx, float2 gy, float2 cos_sin, float2 offset,
    out float3 albedo, out float3 normal, out float height)
{
    float3 uvw = float3(StochasticRotate(uv, cos_sin) + offset, id);
    float2 tile_gx = StochasticRotate(gx, cos_sin);
    float2 tile_gy = StochasticRotate(gy, cos_sin);
    albedo = terrain_surface_albedo.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy).rgb;
    normal = DecodeTerrainSurfaceNormal(terrain_surface_normal.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy));
    normal.xy = StochasticRotate(normal.xy, float2(cos_sin.x, -cos_sin.y));
    height = terrain_surface_height.SampleGrad(terrain_surface_trilinear_repeat_aniso8_sampler, uvw, tile_gx, tile_gy);
}

// Stochastic tiling of a surface (StochasticTiling.hlsl): the three tiles of the triangle grid around uv, blended
void SampleTerrainSurfaceStochastic(int id, float2 uv, float2 gx, float2 gy, out float3 albedo, out float3 normal, out float height)
{
    StochasticTiles tiles = StochasticTilesSetup(uv, gx, gy, (uint)id, terrain_surface_stochastic_rotation);
    float3 albedos[3];
    float3 normals[3];
    float3 heights;
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float tile_height;
        SampleTerrainSurfaceTile(id, uv, tiles.gx, tiles.gy, tiles.cos_sin[i], tiles.offset[i], albedos[i], normals[i], tile_height);
        heights[i] = tile_height;
    }

    float3 weights = StochasticTilesWeights(tiles, heights, terrain_surface_stochastic_contrast);
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

// Color of the LOD view mode: one color per patch size, patch borders darkened. Pixel shader only (uses fwidth)
float3 TerrainLodViewColor(float2 terrain_xz)
{
    static const float3 palette[5] =
    {
        float3(0.2, 0.75, 0.3),  // 1 cell
        float3(0.95, 0.85, 0.2), // 2
        float3(0.95, 0.5, 0.15), // 4
        float3(0.85, 0.2, 0.2),  // 8
        float3(0.55, 0.25, 0.8), // 16 and more
    };

    float2 cells = terrain_xz / width_scale;
    float size = TerrainPatchSize((int2)floor(cells));
    if (size < 0.5)
    {
        return float3(0.1, 0.1, 0.1);
    }

    float3 color = palette[clamp((int)round(log2(size)), 0, 4)];
    // Patches are aligned to their own size, so the position inside the patch is a fraction
    float2 patch_uv = cells / size;
    float2 inside = frac(patch_uv);
    float2 border_pixels = min(inside, 1 - inside) / max(fwidth(patch_uv), 1e-6);
    float border = saturate(min(border_pixels.x, border_pixels.y) - 0.5);
    return color * lerp(0.15, 1, border);
}

// Terrain surface of a pixel. Surfaces are blended by their height maps: within terrain_surface_blend_depth of the
// highest (height + weight), the higher surface wins. The surface normal is planar mapped on the terrain (u = +x,
// v = +z) and returned in the tangent space of the given basis. In the other view modes debug_color is returned.
// world_position: absolute world position, as passed to SampleHeightmapWithGradient.
void SampleTerrainSurface_float(float3 world_position, float3 normal_ws, float3 tangent_ws, float3 bitangent_ws,
    float3 debug_color, out float3 base_color, out float3 normal_ts, out float metallic, out float smoothness)
{
    // Derivatives before any branch
    float2 terrain_xz = TerrainSpacePosition(world_position);
    float2 terrain_ddx = ddx(terrain_xz);
    float2 terrain_ddy = ddy(terrain_xz);

    base_color = debug_color;
    normal_ts = float3(0, 0, 1);
    metallic = 0;
    smoothness = 0.3;
    if (TerrainViewMode() == 4)
    {
        base_color = TerrainLodViewColor(terrain_xz);
        return;
    }
    if (TerrainViewMode() != 0 || terrain_surface_count < 1)
    {
        return;
    }

    CollectTerrainSurfaces(world_position);
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
    float4 far_tint = 0;
    [unroll]
    for (int b = 0; b < TERRAIN_SURFACE_SAMPLES; b++)
    {
        float blend = max(score[b] - threshold, 0);
        blend_total += blend;
        color += albedo[b] * blend;
        detail_normal += detail[b] * blend;
        surface_sum += surface[b] * blend;
        int tint_id = surface_candidate_id[b];
        if (b < samples && tint_id < terrain_far_tint_count)
        {
            far_tint += terrain_far_tint[tint_id] * blend;
        }
    }

    base_color = color / blend_total;
    if (terrain_far_tint_count > 0)
    {
        far_tint /= blend_total;
        float far = smoothstep(terrain_far_tint_range.x, terrain_far_tint_range.y, distance(world_position, terrain_far_tint_center));
        base_color = lerp(base_color, far_tint.rgb, saturate(far_tint.a * far));
    }
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
