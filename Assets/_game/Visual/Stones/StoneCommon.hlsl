#ifndef STONE_COMMON
#define STONE_COMMON

// Data of the small stones, shared by the placement (StonePlacement.compute) and the stone shader (Stone.hlsl).
// Matches Runtime.Stones.StoneRenderer. What every kind of scattered detail has is in ScatterCommon.hlsl

#include "Assets/_game/Visual/Scatter/ScatterCommon.hlsl"

// One stone, written by the placement every frame
struct StoneInstance
{
    float3 position; // absolute world position of the pivot of the shape
    float scale;     // shape units to meters
    float4 rotation; // quaternion, shape space to world
    uint texture_salt; // texture index (low 16 bits) and a random salt for the tiling (high 16 bits)
    uint tint;       // linear rgb8 of half the albedo multiplier
};

// Stones of one stone layer (Core.Stones.StoneLayer), at the index of its ScatterLayer
struct StoneLayer
{
    float2 size;            // meters of the largest dimension, min and max
    float2 slope_alignment; // 0 upright .. 1 along the terrain normal, min and max
    float3 max_rotation;    // radians around x, y, z
    float2 sink;            // in stone sizes, min and max
    uint shape_start;       // its shapes in stone_layer_shapes
    uint shape_count;
    uint texture_start;     // its textures in stone_layer_textures; none: all textures
    uint texture_count;
    float3 tint;            // linear
    float color_variation;  // 0..1
    float cover;            // radius of the ground covered for the grass, in half stone sizes
};

// Colors 0..2 (an albedo multiplier) in rgb8
uint PackStoneTint(float3 tint)
{
    uint3 c = (uint3)round(saturate(tint * 0.5) * 255);
    return c.r | (c.g << 8) | (c.b << 16);
}

float3 UnpackStoneTint(uint packed)
{
    return float3(packed & 255, (packed >> 8) & 255, (packed >> 16) & 255) / 255.0 * 2;
}

float3 RotateByQuaternion(float4 q, float3 v)
{
    float3 t = 2 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

#endif
