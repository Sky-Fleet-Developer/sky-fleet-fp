#ifndef GRASS_COMMON
#define GRASS_COMMON

// Data of the grass, shared by the placement (GrassPlacement.compute) and the plant shader (Grass.hlsl).
// Matches Runtime.Grass.GrassRenderer. What every kind of scattered detail has is in ScatterCommon.hlsl

#include "Assets/_game/Visual/Scatter/ScatterCommon.hlsl"

// One plant, written by the placement every frame
struct GrassBlade
{
    float3 position;  // absolute world position of the root
    float facing;     // radians around y: the direction the plant leans to (blade space +z)
    float2 size;      // width, height in meters
    float bend;       // lean of the top, in plant heights
    uint base_color;  // linear rgb8 at the root
    uint tip_color;   // linear rgb8 at the top
    float2 normal_xz; // terrain normal under the plant (y is positive)
    float2 push;      // lean away from the pushers, in plant heights, world xz
    uint variation;   // PackGrassVariation: its layer, and its random shade and hue for the part colors
};

// Look of the plants of one grass layer (Core.Grass.GrassLayer), at the index of its ScatterLayer
struct GrassLayer
{
    float2 height;         // meters, min and max
    float width;           // meters
    float bend;            // lean of the top, in plant heights
    float color_variation; // 0..1
    float3 base_color;     // linear
    float3 tip_color;      // linear
    uint shape;            // index of the shape: its near batch is shape * 2, its far batch shape * 2 + 1
    uint4 part_colors;     // by submesh of the shape: its colors in grass_palette, first (low 20 bits) and count
                           // (high 12 bits); no colors: the root and tip colors
};

// Sphere pushing the grass (Core.Grass.GrassPusherData)
struct GrassPusher
{
    float3 center;
    float radius;
    float strength;
};

uint PackGrassColor(float3 color)
{
    uint3 c = (uint3)round(saturate(color) * 255);
    return c.r | (c.g << 8) | (c.b << 16);
}

float3 UnpackGrassColor(uint packed)
{
    return float3(packed & 255, (packed >> 8) & 255, (packed >> 16) & 255) / 255.0;
}

// Layer index (16 bits), shade 0..2 and hue (red) 0.5..1.5 of a plant
uint PackGrassVariation(uint layer, float shade, float hue)
{
    uint s = (uint)round(saturate(shade * 0.5) * 255);
    uint h = (uint)round(saturate(hue - 0.5) * 255);
    return (layer & 0xFFFF) | (s << 16) | (h << 24);
}

uint GrassVariationLayer(uint packed)
{
    return packed & 0xFFFF;
}

// The shade and hue as one multiplier of a color
float3 GrassVariationTint(uint packed)
{
    float shade = ((packed >> 16) & 255) / 255.0 * 2;
    float hue = ((packed >> 24) & 255) / 255.0 + 0.5;
    return float3(hue, 1, 1) * shade;
}

#endif
