#ifndef CONTACT
#define CONTACT
#define PI 3.14159265358979323846

#include "Assets/Compute/Wind/Particle.hlsl"
#include "Assets/Compute/Wind/Volume.hlsl"

float smothing_kernel(float d)
{
    float volume = (PI * pow(particle_influence_radius, 4) / 6);
    return (particle_influence_radius - d) * (particle_influence_radius - d) / volume;
}

float smothing_kernel_derivative(float d)
{
    float scale = 12/(pow(particle_influence_radius, 4)*PI);
    return (d - particle_influence_radius) * scale;
}

float SpikyKernelPow2(float dst, float radius)
{
    float scale = 15 / (2 * PI * pow(radius, 5));
    float v = radius - dst;
    return v * v * scale;
}

float SpikyKernelPow3(float dst, float radius)
{
    float scale = 15 / (PI * pow(radius, 6));
    float v = radius - dst;
    return v * v * v * scale;
}

float DerivativeSpikyPow2(float dst, float radius)
{
    float scale = 15 / (pow(radius, 5) * PI);
    float v = radius - dst;
    return -v * scale;
}

float DerivativeSpikyPow3(float dst, float radius)
{
    float scale = 45 / (pow(radius, 6) * PI);
    float v = radius - dst;
    return -v * v * scale;
}

float SmoothingKernelPoly6(float dst, float radius)
{
    float scale = 315 / (64 * PI * pow(abs(radius), 9));
    float v = radius * radius - dst * dst;
    return v * v * v * scale;
}

// Fa = p0VG
// p0-p1=0  равновесие
// F = (p0-p1)Vg
// p = C/V
// F = (C0/V0 - C1/V1)Vg
// V0 = V1 = const =>
// F = (C0 - C1)/V * Vg
// F = (C0 - C1)g


float3 contact(float d, float densityA, float3 positionA, float3 velocityA, float densityB, float3 positionB, float3 velocityB, float convergenceFactor, float pushForce)
{
    float3 direction = (positionB - positionA) / d;
    float deltaP = densityB - densityA;
    float force = DerivativeSpikyPow3(d, particle_influence_radius) * deltaP;

    //float3 vDelta = velocityA - velocityB;
    //float deltaLength = sqrt(dot(vDelta,vDelta));
    /*
    float3 viscosityForce = float3(0, 0, 0);
    if (deltaLength != 0)
    {
        float3 vDeltaNorm = vDelta / deltaLength;
        float convergence = min(1.0f, max(-1.0f, dot(vDeltaNorm, direction)));
        viscosityForce = (-vDelta) * ((1 - convergenceFactor)+abs(convergence)*convergenceFactor) * SmoothingKernelPoly6(d, particle_influence_radius);
    }*/
    float vA = sqrt(dot(velocityA, velocityA));
    float vB = sqrt(dot(velocityB, velocityB));
    float t = 0.5f;
    float interpLength = vA + (vB - vA) * t;
    float3 aNorm = velocityA / vA;
    float3 interpVec = (aNorm + (velocityB / vB - aNorm) * t) * interpLength;
    float3 viscosityForce = (interpVec - velocityA) * viscosity_coefficient * SmoothingKernelPoly6(d, particle_influence_radius);

    float pressureForce = force * pushForce;// / max(densityB, 10);
    
    return direction * pressureForce + viscosityForce * viscosity_coefficient;
}

float3 contact(int a, int b, float dSqr)
{
    float d = sqrt(dSqr);
    if (d * particles[b].density * particles[a].density == 0)
    {
        return float3(0, 0, 0);
    }
    return contact(d, particles[a].density * particles[a].energy, particles[a].position, particles[a].velocity, particles[b].density * particles[b].energy, particles[b].position, particles[b].velocity, 0.85f, push_force);
}

float3 resolve_contact(int a, int b, float dSqr)
{
    return contact(a, b, dSqr);
}

#endif