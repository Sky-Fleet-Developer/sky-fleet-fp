#ifndef CONTACT
#define CONTACT
#define PI 3.14f

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

float3 contact(float d, float densityA, float3 positionA, float3 velocityA, float densityB, float3 positionB, float3 velocityB, float convergenceFactor)
{
    //d = max(d, particle_influence_radius * 0.15f);
    float3 deltaNorm = (positionB - positionA) / d;
    /*float slope = DerivativeSpikyPow2(d, particle_influence_radius);
    deltaNorm *= slope * delta_time * mul;*/
    //float sharedPressure = (densityB - densityA);
    float sharedPressure = (densityA + densityB) * 0.5f;
    float force = DerivativeSpikyPow3(d, particle_influence_radius) * sharedPressure;

    float3 vDelta = velocityA - velocityB;
    float3 vDeltaNorm = vDelta != 0 ? vDelta / sqrt(vDelta.x * vDelta.x + vDelta.y * vDelta.y + vDelta.z * vDelta.z) : float3(0, 0, 0);
    float convergence = min(1.0f, max(-1.0f, dot(vDeltaNorm, deltaNorm)));
    /*float3 viscosityForce = convergence > 0 ?
        (-vDelta) * (0.1f+convergence*0.9f) * SmoothingKernelPoly6(d, particle_influence_radius)
        : float3(0, 0, 0);*/
    float3 viscosityForce = (-vDelta) * ((1 - convergenceFactor)+abs(convergence)*convergenceFactor) * SmoothingKernelPoly6(d, particle_influence_radius);

    float pressureForce = force * push_force / max(densityB, 1000);
    
    return deltaNorm * pressureForce + viscosityForce * viscosity_coefficient;
}

float3 contact(int a, int b, float dSqr, float mul)
{
    float d = sqrt(dSqr);
    if (d == 0 || particles[b].density == 0 || particles[a].density == 0)
    {
        return float3(0, 0, 0);
    }
    return contact(d, particles[a].density, particles[a].position, particles[a].velocity, particles[b].density, particles[b].position, particles[b].velocity, 0.7f) * mul;
}

float3 resolve_contact(int a, int b, float dSqr)
{
    return contact(a, b, dSqr, 0.5f);
}
float3 resolve_neighbour_contact(int a, int b, float dSqr)
{
    return contact(a, b, dSqr, 0.5f);
}

#endif