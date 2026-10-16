#ifndef STONE
#define STONE

// Custom functions of the stone shader graph (Stone.shadergraph). The stones are drawn by Graphics.RenderMeshIndirect
// from the buffer the placement writes, without instancing: a batch mesh holds a copy of its shape for every stone of
// its part of the buffer (Runtime.Scatter.ScatterBatchMesh), and the vertex id tells the copy.
// Textures: tiling arrays of Core.Stones.StoneTextureSet, mapped by UV0 at an even texel density on every stone
// (uv * shape meters per uv * scale * repetitions per meter), optionally with stochastic tiling.

#include "StoneCommon.hlsl"
#include "Assets/_game/Visual/Common/StochasticTiling.hlsl"

StructuredBuffer<StoneInstance> scatter_instances;
float scatter_instance_offset;       // per batch: its first stone in scatter_instances
float scatter_vertices_per_instance; // per batch: vertices of its shape
float stone_uv_meters;               // per batch: shape units per uv unit of its shape
float stone_shape_top;               // per batch: top of its shape above the pivot, shape units

Texture2DArray stone_albedo;
Texture2DArray stone_normal; // x, y in red and green
Texture2DArray stone_mask;   // r metallic, g occlusion, b height, a smoothness
// Inline sampler, defined by its name: each pass may use only some of the arrays
SamplerState stone_trilinear_repeat_aniso8_sampler;
float stone_uv_density;  // texture repetitions per meter
float4 stone_stochastic; // x 1: stochastic tiling, y contrast, z rotation in turns
float4 stone_look;       // x smoothness multiplier, y normal strength
// Ambient occlusion per vertex (all 0 when off): x strength of the vertex color (grayscale), y darkening at the
// bottom, z share of the shape height over which the bottom darkening fades, w how much it darkens the color too
float4 stone_occlusion;

// Occlusion of a vertex, 0..1: baked in the vertex color of the shape, and darker towards the bottom of the shape
float StoneVertexOcclusion(float3 mesh_position, float4 mesh_color)
{
    float baked = lerp(1, dot(mesh_color.rgb, 1.0 / 3), stone_occlusion.x);
    float height = saturate(mesh_position.y / max(stone_shape_top * stone_occlusion.z, 1e-4));
    float ground = lerp(1 - stone_occlusion.y, 1, height * height * (3 - 2 * height));
    return baked * ground;
}

// stone_uv: xy texture uv in repetitions, z texture index, w tiling salt; stone_tint: rgb albedo multiplier, a vertex
// occlusion
void StoneVertex_float(float3 mesh_position, float3 mesh_normal, float3 mesh_tangent, float4 mesh_uv, float4 mesh_color,
    float vertex_id, out float3 position_os, out float3 normal_os, out float3 tangent_os, out float4 stone_uv,
    out float4 stone_tint)
{
#ifdef SHADERGRAPH_PREVIEW
    position_os = mesh_position;
    normal_os = mesh_normal;
    tangent_os = mesh_tangent;
    stone_uv = float4(mesh_uv.xy, 0, 0);
    stone_tint = 1;
#else
    uint slot = (uint)vertex_id / (uint)max(scatter_vertices_per_instance, 1);
    StoneInstance stone = scatter_instances[(uint)scatter_instance_offset + slot];
    float3 position = stone.position + RotateByQuaternion(stone.rotation, mesh_position * stone.scale);
    position_os = TransformWorldToObject(GetCameraRelativePositionWS(position));
    normal_os = TransformWorldToObjectNormal(RotateByQuaternion(stone.rotation, mesh_normal));
    tangent_os = TransformWorldToObjectDir(RotateByQuaternion(stone.rotation, mesh_tangent));

    // A random offset per stone, so stones sharing a texture don't show the same patch
    uint salt = stone.texture_salt >> 16;
    float2 offset = float2(salt & 255, salt >> 8) / 256.0;
    stone_uv = float4(mesh_uv.xy * (stone_uv_meters * stone.scale * stone_uv_density) + offset,
        stone.texture_salt & 0xFFFF, salt);
    stone_tint = float4(UnpackStoneTint(stone.tint), StoneVertexOcclusion(mesh_position, mesh_color));
#endif
}

void SampleStoneTile(float3 uvw, float2 gx, float2 gy, out float3 albedo, out float3 normal, out float4 mask)
{
    albedo = stone_albedo.SampleGrad(stone_trilinear_repeat_aniso8_sampler, uvw, gx, gy).rgb;
    float2 xy = stone_normal.SampleGrad(stone_trilinear_repeat_aniso8_sampler, uvw, gx, gy).rg * 2 - 1;
    normal = float3(xy, sqrt(saturate(1 - dot(xy, xy))));
    mask = stone_mask.SampleGrad(stone_trilinear_repeat_aniso8_sampler, uvw, gx, gy);
}

void StoneFragment_float(float4 stone_uv, float4 stone_tint, out float3 base_color, out float3 normal_ts,
    out float metallic, out float smoothness, out float occlusion)
{
#ifdef SHADERGRAPH_PREVIEW
    base_color = 0.5;
    normal_ts = float3(0, 0, 1);
    metallic = 0;
    smoothness = 0.3;
    occlusion = 1;
#else
    // Derivatives before any branch
    float2 uv = stone_uv.xy;
    float2 gx = ddx(uv);
    float2 gy = ddy(uv);
    float layer = round(stone_uv.z);

    float3 albedo;
    float3 normal;
    float4 mask;
    if (stone_stochastic.x > 0.5)
    {
        StochasticTiles tiles = StochasticTilesSetup(uv, gx, gy, (uint)round(stone_uv.w), stone_stochastic.z);
        float3 albedos[3];
        float3 normals[3];
        float4 masks[3];
        float3 heights;
        [unroll]
        for (int i = 0; i < 3; i++)
        {
            SampleStoneTile(float3(StochasticTileUV(tiles, i, uv), layer), StochasticTileGrad(tiles, i, tiles.gx),
                StochasticTileGrad(tiles, i, tiles.gy), albedos[i], normals[i], masks[i]);
            normals[i].xy = StochasticNormalBack(tiles, i, normals[i].xy);
            heights[i] = masks[i].b;
        }
        float3 weights = StochasticTilesWeights(tiles, heights, stone_stochastic.y);
        albedo = albedos[0] * weights.x + albedos[1] * weights.y + albedos[2] * weights.z;
        normal = normals[0] * weights.x + normals[1] * weights.y + normals[2] * weights.z;
        mask = masks[0] * weights.x + masks[1] * weights.y + masks[2] * weights.z;
    }
    else
    {
        SampleStoneTile(float3(uv, layer), gx, gy, albedo, normal, mask);
    }

    // HDRP occlusion only darkens the indirect light; some of it goes into the color, like a contact shadow
    float vertex_occlusion = stone_tint.a;
    base_color = albedo * stone_tint.rgb * lerp(1, vertex_occlusion, stone_occlusion.w);
    normal_ts = normalize(float3(normal.xy * stone_look.y, max(normal.z, 1e-3)));
    metallic = mask.r;
    occlusion = mask.g * vertex_occlusion;
    smoothness = saturate(mask.a * stone_look.x);
#endif
}

#endif
