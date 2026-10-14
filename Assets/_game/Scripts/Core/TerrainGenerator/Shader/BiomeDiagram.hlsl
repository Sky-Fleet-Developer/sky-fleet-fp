#ifndef BIOME_DIAGRAM
#define BIOME_DIAGRAM

// Power diagram of the biome sites. Mirrors Core.TerrainGenerator.Biomes.BiomeDiagram.

struct BiomeSite
{
    float2 position; // terrain space x, z in meters
    float radius;
    float blend_distance;
    int biome;       // index in blending order
    float2 rotation; // cos, sin of the site rotation around Y
    int blend_mode;  // BIOME_BLEND_*
    float height_offset; // meters, added to the heights of the biome data placed by this site
};

// Core.TerrainGenerator.Biomes.BiomeBlendMode
#define BIOME_BLEND_NORMAL 0
#define BIOME_BLEND_OVERRIDE 1
#define BIOME_BLEND_ADD 2
#define BIOME_BLEND_SUBTRACT 3
#define BIOME_BLEND_MAX 4
#define BIOME_BLEND_MIN 5
#define BIOME_BLEND_MULTIPLY 6

bool BiomeIsLayer(BiomeSite site)
{
    return site.blend_mode != BIOME_BLEND_NORMAL;
}

// True when c lowers the weight of a: normal biomes ignore the layers
bool BiomeCompetes(BiomeSite a, BiomeSite c)
{
    return c.biome != a.biome && (BiomeIsLayer(a) || !BiomeIsLayer(c));
}

// Height below a layer combined with the layer height at full weight
float BiomeApplyLayer(int blend_mode, float below, float layer)
{
    switch (blend_mode)
    {
        case BIOME_BLEND_ADD: return below + layer;
        case BIOME_BLEND_SUBTRACT: return below - layer;
        case BIOME_BLEND_MAX: return max(below, layer);
        case BIOME_BLEND_MIN: return min(below, layer);
        case BIOME_BLEND_MULTIPLY: return below * layer;
        default: return layer;
    }
}

// Terrain space position -> unrotated space of the site, relative to the site
float2 BiomeSiteToLocal(BiomeSite site, float2 position)
{
    float2 delta = position - site.position;
    return float2(delta.x * site.rotation.x - delta.y * site.rotation.y, delta.x * site.rotation.y + delta.y * site.rotation.x);
}

#define BIOME_COMPETITOR_FLAG 0x80000000u

StructuredBuffer<BiomeSite> biome_sites;
StructuredBuffer<uint> biome_site_indices; // per layer: sites that matter, in blending order; may carry BIOME_COMPETITOR_FLAG
StructuredBuffer<uint2> biome_layer_ranges; // per layer: start and count in biome_site_indices

// Border noise, bound by Core.TerrainGenerator.Settings.BiomeEdgeNoise. Weights are evaluated at a position shifted
// by fractal gradient noise, so the borders bend; the biome data is still sampled at the true position
float biome_warp_amplitude; // meters, largest shift along each axis; 0 = no warp
float biome_warp_frequency; // 1 / wavelength in meters
int biome_warp_octaves;
float2 biome_warp_offset;

uint BiomeHash(int2 cell, uint channel)
{
    // pcg2d
    uint2 v = asuint(cell) * 1664525u + 1013904223u + channel * 2654435769u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    return v.x;
}

float BiomeGradientDot(int2 cell, uint channel, float2 delta)
{
    float angle = BiomeHash(cell, channel) * (6.28318530718 / 4294967296.0);
    return dot(float2(cos(angle), sin(angle)), delta);
}

// Gradient noise in [-1, 1]
float BiomeNoise(float2 p, uint channel)
{
    int2 cell = (int2)floor(p);
    float2 f = p - cell;
    float2 u = f * f * f * (f * (f * 6 - 15) + 10);
    float n00 = BiomeGradientDot(cell, channel, f);
    float n10 = BiomeGradientDot(cell + int2(1, 0), channel, f - float2(1, 0));
    float n01 = BiomeGradientDot(cell + int2(0, 1), channel, f - float2(0, 1));
    float n11 = BiomeGradientDot(cell + int2(1, 1), channel, f - float2(1, 1));
    return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y) * 1.41421356;
}

// Position at which biome weights are evaluated. Shifted by at most biome_warp_amplitude along each axis
float2 BiomeWarp(float2 position)
{
    if (biome_warp_amplitude <= 0)
    {
        return position;
    }

    float2 p = position * biome_warp_frequency + biome_warp_offset;
    float2 sum = 0;
    float weight = 1;
    float total = 0;
    for (int i = 0; i < biome_warp_octaves; i++)
    {
        sum += weight * float2(BiomeNoise(p, 0), BiomeNoise(p, 1));
        total += weight;
        weight *= 0.5;
        p = p * 2 + float2(13.7, 7.3);
    }
    return position + clamp(sum / max(total, 1e-5), -1, 1) * biome_warp_amplitude;
}

// Signed distance from the power bisector of a and c, positive on the side of a
float BiomeSignedDistance(BiomeSite a, BiomeSite c, float2 position)
{
    float2 delta = a.position - c.position;
    float len = length(delta);
    if (len < 1e-3)
    {
        return a.radius >= c.radius ? 3.0e38 : -3.0e38;
    }

    float2 middle = (a.position + c.position) * 0.5;
    return dot(position - middle, delta / len) + (a.radius * a.radius - c.radius * c.radius) / (2.0 * len);
}

float BiomeTransition(BiomeSite a, BiomeSite c, float sd)
{
    float band = a.blend_distance + c.blend_distance;
    if (band <= 0)
    {
        return sd >= 0 ? 1 : 0;
    }
    return smoothstep(0, 1, saturate((sd + c.blend_distance) / band));
}

// Weight of the site a at a position, against the other entries of the layer range. The borders are bent by BiomeWarp
float BiomeSiteWeight(BiomeSite a, uint2 range, float2 position)
{
    position = BiomeWarp(position);
    float weight = 1;
    for (uint j = 0; j < range.y && weight > 0; j++)
    {
        BiomeSite c = biome_sites[biome_site_indices[range.x + j] & ~BIOME_COMPETITOR_FLAG];
        if (BiomeCompetes(a, c))
        {
            weight *= BiomeTransition(a, c, BiomeSignedDistance(a, c, position));
        }
    }
    return weight;
}

#endif
