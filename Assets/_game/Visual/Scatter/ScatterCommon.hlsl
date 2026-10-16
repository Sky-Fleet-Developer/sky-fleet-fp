#ifndef SCATTER_COMMON
#define SCATTER_COMMON

// Data shared by every kind of scattered detail (grass, small stones): by their placement (ScatterPlacement.hlsl) and
// their shaders. Matches Runtime.Scatter.ScatterPipeline, ScatterLayerTable and ScatterSystem.

// What every layer has, for picking the layer of a candidate (Core.Scatter.IScatterLayer). The layers of a surface type
// are listed one after another; the layers of the kind (its own buffer) have the same indices
struct ScatterLayer
{
    float density; // details per square meter
    float4 noise;  // density patches: x frequency (0: none), y..z noise values of the edges, w 1: inverted
    uint noise_seed;
    float min_weight; // weight of its surface below which it doesn't appear
};

// A part of the instance buffer drawn with one mesh, which holds a copy of its shape for every slot of the part
struct ScatterBatch
{
    uint offset;               // first instance in the buffer
    uint capacity;             // instances
    uint indices_per_instance; // of the shape mesh
    uint padding;
};

// Area kept clear of details (Core.Scatter.ScatterAreaData)
struct ScatterArea
{
    float4x4 world_to_local;
    float3 size;     // box: size; sphere: x radius; polygon: y bottom, z top
    float edge;      // meters outside over which the details grow back
    uint shape;      // 0 box, 1 sphere, 2 polygon
    uint polygon_start;
    uint polygon_count;
};

uint ScatterHash(uint3 v)
{
    // pcg3d
    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    return v.x ^ v.y ^ v.z;
}

#endif
