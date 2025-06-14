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

const static float targetDensity = 0;

void contact(int a, int b, float dSqr, float mul)
{
    float3 delta = particles[b].position - particles[a].position;
    float d = sqrt(dSqr);
    //d = max(d, particle_influence_radius * 0.15f);
    if (d == 0)
    {
        return;
    }
    float dInv = 1.0f / d;
    float3 deltaNorm = delta * dInv;
    /*float slope = DerivativeSpikyPow2(d, particle_influence_radius);
    deltaNorm *= slope * delta_time * mul;*/
    float sharedPressure = (particles[a].density + particles[b].density) * 0.5f;
    float force = DerivativeSpikyPow2(d, particle_influence_radius) * sharedPressure;

    float3 otherVelocity = particles[b].velocity;
    float3 velocity = particles[a].velocity;
    float3 vDelta = velocity - otherVelocity;
    float3 vDeltaNorm = vDelta != 0 ? vDelta / sqrt(dot(vDelta, vDelta)) : float3(0, 0, 0);
    float convergence = min(1.0f, max(-1.0f, dot(vDeltaNorm, deltaNorm)));
    /*float3 viscosityForce = convergence > 0 ?
        (-vDelta) * (0.1f+convergence*0.9f) * SmoothingKernelPoly6(d, particle_influence_radius)
        : float3(0, 0, 0);*/
    float3 viscosityForce = (-vDelta) * (0.3f+abs(convergence)*0.7f) * SmoothingKernelPoly6(d, particle_influence_radius);

    float pressureForce = force / particles[b].density * mul * push_force;
    
    particles[a].gradient += deltaNorm * pressureForce + viscosityForce * viscosity_coefficient;
}

void resolve_contact(int a, int b, float dSqr)
{
    contact(a, b, dSqr, 0.5f);
}
void resolve_neighbour_contact(int a, int b, float dSqr)
{
    contact(a, b, dSqr, 0.5f);
}

#endif