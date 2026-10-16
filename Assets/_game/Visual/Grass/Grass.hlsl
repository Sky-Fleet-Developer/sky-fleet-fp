#ifndef GRASS
#define GRASS

// Custom functions of the grass blade shader graph (Grass.shadergraph). The blades are drawn by
// Graphics.RenderMeshIndirect from the buffer the placement writes, without instancing: the mesh holds a copy of the
// blade for every slot of the buffer and the placement sets how many of them are drawn. Mesh positions are in blade
// space: x across (-0.5..0.5, tapering), y along the blade (0 root, 1 tip), z the slot of the blade.
// (Procedural instancing doesn't work from a shader graph: its pragma comes after the instancing library.)

#include "GrassCommon.hlsl"

StructuredBuffer<GrassBlade> grass_blades;
float4 grass_wind;       // xy direction (unit), z strength in blade heights, w wave length in meters
float4 grass_wind_shape; // x speed in m/s, y gustiness 0..1, z flutter 0..1
float4 grass_look;       // x darkening at the root, y smoothness, z how much the blades take the terrain normal

// Lean the wind adds to a blade, in blade heights: travelling waves along the wind, gusts and a little flutter
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
void GrassVertex_float(float3 mesh_position, out float3 position_os, out float3 normal_os, out float3 tangent_os,
    out float4 grass_color)
{
#ifdef SHADERGRAPH_PREVIEW
    position_os = float3(mesh_position.xy, 0);
    normal_os = float3(0, 0, 1);
    tangent_os = float3(1, 0, 0);
    grass_color = float4(0.2, 0.45, 0.1, 1);
#else
    GrassBlade blade = grass_blades[(uint)mesh_position.z];
    float t = mesh_position.y;
    float2 facing = float2(sin(blade.facing), cos(blade.facing));
    float2 across = float2(facing.y, -facing.x);

    // The blade bends along a parabola; dividing by the lean keeps its length about the same
    float2 lean = facing * blade.bend + GrassWind(blade.position, _TimeParameters.x);
    float shorten = rsqrt(1 + dot(lean, lean) * t * t);
    float height = blade.size.y;
    float2 lean_offset = lean * (height * t * t * shorten);
    float2 width_offset = across * (mesh_position.x * blade.size.x);
    float3 position = blade.position + float3(lean_offset.x + width_offset.x, height * t * shorten, lean_offset.y + width_offset.y);

    // Mostly the normal of the ground under the blade, so a meadow is lit like a soft surface; rounded across the blade
    float3 terrain_normal = float3(blade.normal_xz.x, sqrt(saturate(1 - dot(blade.normal_xz, blade.normal_xz))), blade.normal_xz.y);
    float3 blade_normal = normalize(float3(-facing.x, 0.5, -facing.y) + float3(across.x, 0, across.y) * (mesh_position.x * 2));
    float3 normal = normalize(lerp(blade_normal, terrain_normal, grass_look.z));

    position_os = TransformWorldToObject(GetCameraRelativePositionWS(position));
    normal_os = TransformWorldToObjectNormal(normal);
    tangent_os = TransformWorldToObjectDir(float3(across.x, 0, across.y));

    // Fake self shadowing: the roots, deep in the grass, get little light
    float light = lerp(1 - grass_look.x, 1, sqrt(t));
    float3 color = lerp(UnpackGrassColor(blade.base_color), UnpackGrassColor(blade.tip_color), t);
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
