#ifndef MATH3
#define MATH3

float dot(float3 a, float3 b)
{
    return a.x * b.x + a.y * b.y + a.z * b.z;
}

float3 cross(float3 a, float3 b)
{
    return float3(
        a.y * b.z - a.z * b.y,
        a.z * b.x - a.x * b.z,
        a.x * b.y - a.y * b.x
    );
}

float3 reflect(float3 value, float3 normal, float bounce = 1.0f)
{
    float scalarProduct = dot(value, normal) * (bounce * 2.0f);
    
    return value - scalarProduct * normal;
}

float3 sphere_to_rect(float2 value)
{
    float x = sin(value.y) * cos(value.x);
    float y = cos(value.y);
    float z = sin(value.y) * sin(value.x);
    
    return float3(x, y, z);
}

float2 rect_to_sphere(float3 value)
{
    //float radius = sqrt(value.x*value.x + value.y*value.y + value.z*value.z);
    return float2(atan2(value.z, value.x), acos(value.y));
}

static const uint neighbours_1_count = 13;
static const uint3 neighbours_1[13] =
{
    uint3(1, 0, 0),
    uint3(0, 1, 0),
    uint3(0, 0, 1),
    uint3(1, 0, 1),
    uint3(1, 1, 0),
    uint3(0, 1, 1),
    uint3(1, 1, 1),
    uint3(-1, 1, 1),
    uint3(-1, 0, 1),
    uint3(-1, -1, 1),
    uint3(1, -1, 0),
    uint3(1, -1, 1),
    uint3(0, -1, 1),
};

static const uint neighbours_1_all_count = 27;
static const int3 neighbours_1_all[27] =
{
    int3(-1, -1, -1),
    int3(-1, -1, 0),
    int3(-1, -1, 1),
    int3(-1, 0, -1),
    int3(-1, 0, 0),
    int3(-1, 0, 1),
    int3(-1, 1, -1),
    int3(-1, 1, 0),
    int3(-1, 1, 1),
    int3(0, -1, -1),
    int3(0, -1, 0),
    int3(0, -1, 1),
    int3(0, 0, -1),
    int3(0, 0, 0),
    int3(0, 0, 1),
    int3(0, 1, -1),
    int3(0, 1, 0),
    int3(0, 1, 1),
    int3(1, -1, -1),
    int3(1, -1, 0),
    int3(1, -1, 1),
    int3(1, 0, -1),
    int3(1, 0, 0),
    int3(1, 0, 1),
    int3(1, 1, -1),
    int3(1, 1, 0),
    int3(1, 1, 1)
};

static const uint neighbours_3_count = 171;
static const uint3 neighbours_3[171] =
{
    uint3(3, 3, 3),
    uint3(3, 3, 2),
    uint3(3, 2, 3),
    uint3(2, 3, 3),
    uint3(3, 3, 1),
    uint3(3, 2, 2),
    uint3(3, 1, 3),
    uint3(2, 3, 2),
    uint3(2, 2, 3),
    uint3(1, 3, 3),
    uint3(3, 3, 0),
    uint3(3, 2, 1),
    uint3(3, 1, 2),
    uint3(3, 0, 3),
    uint3(2, 3, 1),
    uint3(2, 2, 2),
    uint3(2, 1, 3),
    uint3(1, 3, 2),
    uint3(1, 2, 3),
    uint3(0, 3, 3),
    uint3(3, 3, -1),
    uint3(3, 2, 0),
    uint3(3, 1, 1),
    uint3(3, 0, 2),
    uint3(3, -1, 3),
    uint3(2, 3, 0),
    uint3(2, 2, 1),
    uint3(2, 1, 2),
    uint3(2, 0, 3),
    uint3(1, 3, 1),
    uint3(1, 2, 2),
    uint3(1, 1, 3),
    uint3(0, 3, 2),
    uint3(0, 2, 3),
    uint3(-1, 3, 3),
    uint3(3, 3, -2),
    uint3(3, 2, -1),
    uint3(3, 1, 0),
    uint3(3, 0, 1),
    uint3(3, -1, 2),
    uint3(3, -2, 3),
    uint3(2, 3, -1),
    uint3(2, 2, 0),
    uint3(2, 1, 1),
    uint3(2, 0, 2),
    uint3(2, -1, 3),
    uint3(1, 3, 0),
    uint3(1, 2, 1),
    uint3(1, 1, 2),
    uint3(1, 0, 3),
    uint3(0, 3, 1),
    uint3(0, 2, 2),
    uint3(0, 1, 3),
    uint3(-1, 3, 2),
    uint3(-1, 2, 3),
    uint3(-2, 3, 3),
    uint3(3, 3, -3),
    uint3(3, 2, -2),
    uint3(3, 1, -1),
    uint3(3, 0, 0),
    uint3(3, -1, 1),
    uint3(3, -2, 2),
    uint3(3, -3, 3),
    uint3(2, 3, -2),
    uint3(2, 2, -1),
    uint3(2, 1, 0),
    uint3(2, 0, 1),
    uint3(2, -1, 2),
    uint3(2, -2, 3),
    uint3(1, 3, -1),
    uint3(1, 2, 0),
    uint3(1, 1, 1),
    uint3(1, 0, 2),
    uint3(1, -1, 3),
    uint3(0, 3, 0),
    uint3(0, 2, 1),
    uint3(0, 1, 2),
    uint3(0, 0, 3),
    uint3(-1, 3, 1),
    uint3(-1, 2, 2),
    uint3(-1, 1, 3),
    uint3(-2, 3, 2),
    uint3(-2, 2, 3),
    uint3(-3, 3, 3),
    uint3(3, 2, -3),
    uint3(3, 1, -2),
    uint3(3, 0, -1),
    uint3(3, -1, 0),
    uint3(3, -2, 1),
    uint3(3, -3, 2),
    uint3(2, 3, -3),
    uint3(2, 2, -2),
    uint3(2, 1, -1),
    uint3(2, 0, 0),
    uint3(2, -1, 1),
    uint3(2, -2, 2),
    uint3(2, -3, 3),
    uint3(1, 3, -2),
    uint3(1, 2, -1),
    uint3(1, 1, 0),
    uint3(1, 0, 1),
    uint3(1, -1, 2),
    uint3(1, -2, 3),
    uint3(0, 3, -1),
    uint3(0, 2, 0),
    uint3(0, 1, 1),
    uint3(0, 0, 2),
    uint3(0, -1, 3),
    uint3(-1, 3, 0),
    uint3(-1, 2, 1),
    uint3(-1, 1, 2),
    uint3(-1, 0, 3),
    uint3(-2, 3, 1),
    uint3(-2, 2, 2),
    uint3(-2, 1, 3),
    uint3(-3, 3, 2),
    uint3(-3, 2, 3),
    uint3(3, 1, -3),
    uint3(3, 0, -2),
    uint3(3, -1, -1),
    uint3(3, -2, 0),
    uint3(3, -3, 1),
    uint3(2, 2, -3),
    uint3(2, 1, -2),
    uint3(2, 0, -1),
    uint3(2, -1, 0),
    uint3(2, -2, 1),
    uint3(2, -3, 2),
    uint3(1, 3, -3),
    uint3(1, 2, -2),
    uint3(1, 1, -1),
    uint3(1, 0, 0),
    uint3(1, -1, 1),
    uint3(1, -2, 2),
    uint3(1, -3, 3),
    uint3(0, 3, -2),
    uint3(0, 2, -1),
    uint3(0, 1, 0),
    uint3(0, 0, 1),
    uint3(0, -1, 2),
    uint3(0, -2, 3),
    uint3(-1, 3, -1),
    uint3(-1, 2, 0),
    uint3(-1, 1, 1),
    uint3(-1, 0, 2),
    uint3(-1, -1, 3),
    uint3(-2, 3, 0),
    uint3(-2, 2, 1),
    uint3(-2, 1, 2),
    uint3(-2, 0, 3),
    uint3(-3, 3, 1),
    uint3(-3, 2, 2),
    uint3(-3, 1, 3),
    uint3(3, 0, -3),
    uint3(3, -1, -2),
    uint3(3, -2, -1),
    uint3(3, -3, 0),
    uint3(2, 1, -3),
    uint3(2, 0, -2),
    uint3(2, -1, -1),
    uint3(2, -2, 0),
    uint3(2, -3, 1),
    uint3(1, 2, -3),
    uint3(1, 1, -2),
    uint3(1, 0, -1),
    uint3(1, -1, 0),
    uint3(1, -2, 1),
    uint3(1, -3, 2),
    uint3(0, 3, -3),
    uint3(0, 2, -2),
    uint3(0, 1, -1),
};

#endif