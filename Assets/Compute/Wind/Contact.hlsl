#ifndef CONTACT
#define CONTACT
#define PI 3.14f

#include "Assets/Compute/Wind/Particle.hlsl"

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

void contact(int a, int b, float dSqr, float mul)
{
    particle p_a = particles[a];
    particle p_b = particles[b];
    float3 delta = p_b.position - p_a.position;
    float d = sqrt(dSqr);
    if (d == 0)
    {
        return;
    }
    float dInv = 1.0f / d;
    float3 deltaNorm = delta * dInv;
    float slope = DerivativeSpikyPow2(d, particle_influence_radius);
    deltaNorm *= slope * delta_time * mul;
    //float density =
    p_a.gradient += deltaNorm;
    p_b.gradient -= deltaNorm;
    particles[a] = p_a;
    particles[b] = p_b;
}

void resolve_contact(int a, int b, float dSqr)
{
    contact(a, b, dSqr, 0.5f);
}
void resolve_neighbour_contact(int a, int b, float dSqr)
{
    contact(a, b, dSqr, 1);
}

#endif