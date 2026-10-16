#ifndef GRASS_COMMON
#define GRASS_COMMON

// Data of the grass blades, shared by the placement (GrassPlacement.compute) and the blade shader (Grass.hlsl).
// Matches Runtime.Grass.GrassRenderer

// One blade, written by the placement every frame
struct GrassBlade
{
    float3 position;  // absolute world position of the root
    float facing;     // radians around y: the direction the blade leans to
    float2 size;      // width, height in meters
    float bend;       // lean of the tip, in blade heights
    uint base_color;  // linear rgb8 at the root
    uint tip_color;   // linear rgb8 at the tip
    float2 normal_xz; // terrain normal under the blade (y is positive)
    float padding;
};

// Grass of a surface type, by surface id
struct GrassSurface
{
    float density;         // blades per square meter
    float2 height;         // meters, min and max
    float width;           // meters
    float bend;            // lean of the tip, in blade heights
    float color_variation; // 0..1
    float3 base_color;     // linear
    float3 tip_color;      // linear
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

#endif
