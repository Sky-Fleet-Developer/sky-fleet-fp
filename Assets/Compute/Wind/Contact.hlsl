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

void contact(int a, int b, float dSqr, float mul)
{
    particle p_a = particles[a];
    particle p_b = particles[b];
    float3 delta = p_b.position - p_a.position;
    float d = sqrt(dSqr);
    float dInv = 1.0f / d;
    float3 deltaNorm = delta * dInv;
    float slope = smothing_kernel_derivative(d);
    deltaNorm *= slope * delta_time;
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