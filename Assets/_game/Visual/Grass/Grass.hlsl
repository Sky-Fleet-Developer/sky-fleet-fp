#ifndef GRASS
#define GRASS

// Custom functions of the grass shader graph (Grass.shadergraph). The plants are drawn by Graphics.RenderMeshIndirect
// from the buffer the placement writes, without instancing: a batch mesh holds a copy of its shape for every plant of
// its part of the buffer, and the placement sets how many of them are drawn. The vertex id tells the copy, so the
// shape mesh keeps its own positions, normals and colors. Vertex color alpha holds the submesh of the vertex, the part
// of the shape its layer may give its own color (ScatterBatchMesh).
// Shape space: x across and z forward (scaled by the plant width), y up from 0 at the root to 1 at the top (scaled by
// the plant height); the plant leans to +z.
// (Procedural instancing doesn't work from a shader graph: its pragma comes after the instancing library.)

#include "GrassCommon.hlsl"

StructuredBuffer<GrassBlade> scatter_instances;
StructuredBuffer<GrassLayer> grass_layers;
StructuredBuffer<uint> grass_palette; // own colors of the parts of the layers, linear rgb8
float scatter_instance_offset;       // per batch: its first plant in scatter_instances
float scatter_vertices_per_instance; // per batch: vertices of its shape
float4 grass_wind;       // xy direction (unit), z strength in plant heights, w wave length in meters
float4 grass_wind_shape; // x speed in m/s, y gustiness 0..1, z flutter 0..1
float4 grass_look;       // x darkening at the root, y smoothness, z how much the plants take the terrain normal

// Lean the wind adds to a plant, in plant heights: travelling waves along the wind, gusts and a little flutter
float2 GrassWind(float3 root, float time)
{
    float distance_along = dot(root.xz, grass_wind.xy);
    float phase = (distance_along - time * grass_wind_shape.x) / max(grass_wind.w, 0.01);
    float wave = sin(phase * 6.28318) * 0.5 + 0.5;
    float gust = sin(phase * 2.39996 + root.x * 0.05 + root.z * 0.07) * 0.5 + 0.5;
    float strength = grass_wind.z * lerp(1, wave * (0.5 + gust), grass_wind_shape.y);
    float flutter = sin(time * 7.3 + root.x * 3.1 + root.z * 2.7) * grass_wind_shape.z * grass_wind.z * 0.3;
    return grass_wind.xy * (strength + flutter);
}

// grass_color: rgb color of the vertex, a light (fake self shadowing), for GrassFragment
void GrassVertex_float(float3 mesh_position, float3 mesh_normal, float4 vertex_color, float vertex_id,
    out float3 position_os, out float3 normal_os, out float3 tangent_os, out float4 grass_color)
{
#ifdef SHADERGRAPH_PREVIEW
    position_os = mesh_position;
    normal_os = mesh_normal;
    tangent_os = float3(1, 0, 0);
    grass_color = float4(0.2, 0.45, 0.1, 1);
#else
    uint slot = (uint)vertex_id / (uint)max(scatter_vertices_per_instance, 1);
    GrassBlade blade = scatter_instances[(uint)scatter_instance_offset + slot];
    float t = saturate(mesh_position.y);
    float2 forward = float2(sin(blade.facing), cos(blade.facing));
    float2 across = float2(forward.y, -forward.x);

    // Higher parts bend along a parabola; dividing by the lean keeps the plant about as long
    float2 lean = forward * blade.bend + blade.push + GrassWind(blade.position, _TimeParameters.x);
    float shorten = rsqrt(1 + dot(lean, lean) * t * t);
    float height = blade.size.y;
    float2 lean_offset = lean * (height * mesh_position.y * mesh_position.y * shorten);
    float2 shape_offset = (across * mesh_position.x + forward * mesh_position.z) * blade.size.x;
    float3 position = blade.position + float3(lean_offset.x + shape_offset.x, height * mesh_position.y * shorten,
        lean_offset.y + shape_offset.y);

    // The normal of the shape, turned with the plant, leans towards the ground under it so a meadow is lit softly
    float3 across3 = float3(across.x, 0, across.y);
    float3 forward3 = float3(forward.x, 0, forward.y);
    float3 shape_normal = normalize(across3 * mesh_normal.x + float3(0, mesh_normal.y, 0) + forward3 * mesh_normal.z);
    float3 terrain_normal = float3(blade.normal_xz.x, sqrt(saturate(1 - dot(blade.normal_xz, blade.normal_xz))), blade.normal_xz.y);
    float3 normal = normalize(lerp(shape_normal, terrain_normal, grass_look.z));

    position_os = TransformWorldToObject(GetCameraRelativePositionWS(position));
    normal_os = TransformWorldToObjectNormal(normal);
    tangent_os = TransformWorldToObjectDir(across3);

    // Fake self shadowing: the roots, deep in the grass, get little light
    float light = lerp(1 - grass_look.x, 1, sqrt(t));
    float3 color = lerp(UnpackGrassColor(blade.base_color), UnpackGrassColor(blade.tip_color), t);
    uint part = (uint)round(vertex_color.a * 255);
    if (part < 4)
    {
        uint part_colors = grass_layers[GrassVariationLayer(blade.variation)].part_colors[part];
        uint count = part_colors >> 20;
        if (count > 0)
        {
            // Random per plant and part, the same every frame: the plants stay put
            uint pick = ScatterHash(uint3(asuint(blade.position.x), asuint(blade.position.z), part)) % count;
            color = UnpackGrassColor(grass_palette[(part_colors & 0xFFFFF) + pick]) * GrassVariationTint(blade.variation);
        }
    }
    color *= vertex_color.rgb;
    grass_color = float4(color * light, light);
#endif
}

void GrassFragment_float(float4 grass_color, out float3 base_color, out float smoothness, out float occlusion)
{
    base_color = grass_color.rgb;
    occlusion = grass_color.a;
    smoothness = grass_look.y;
}

#endif
