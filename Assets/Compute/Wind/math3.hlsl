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

float3 reflect(float3 value, float3 normal)
{
    float scalarProduct = 2.0f * dot(value, normal);
    
    return value - scalarProduct * normal;
}

#endif