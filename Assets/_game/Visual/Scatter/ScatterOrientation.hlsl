#ifndef SCATTER_ORIENTATION
#define SCATTER_ORIENTATION

// Orientation of scattered objects (stones, rocks) on the terrain, as quaternions (xyz vector, w scalar)

float4 QuaternionMultiply(float4 a, float4 b)
{
    return float4(a.w * b.xyz + b.w * a.xyz + cross(a.xyz, b.xyz), a.w * b.w - dot(a.xyz, b.xyz));
}

float4 AxisAngle(float3 axis, float angle)
{
    return float4(axis * sin(angle * 0.5), cos(angle * 0.5));
}

// Shortest rotation from +y to a unit vector
float4 RotationFromUp(float3 to)
{
    if (to.y < -0.9999)
    {
        return float4(1, 0, 0, 0);
    }
    float s = sqrt((1 + to.y) * 2);
    return float4(float3(to.z, 0, -to.x) / s, s * 0.5);
}

// Up between upright and the terrain normal by alignment 0..1, then a turn (angles.y) and tilts (angles.z, angles.x)
// around it, radians. Returns the rotation, and the up direction in up
float4 ScatterOrientation(float3 terrain_normal, float alignment, float3 angles, out float3 up)
{
    up = normalize(lerp(float3(0, 1, 0), terrain_normal, alignment));
    float4 tilt = QuaternionMultiply(AxisAngle(float3(1, 0, 0), angles.x), AxisAngle(float3(0, 0, 1), angles.z));
    return QuaternionMultiply(RotationFromUp(up), QuaternionMultiply(tilt, AxisAngle(float3(0, 1, 0), angles.y)));
}

#endif
