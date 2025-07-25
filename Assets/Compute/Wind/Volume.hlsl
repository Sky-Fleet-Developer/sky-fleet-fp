#ifndef VOLUME
#define VOLUME
#include "Assets/Compute/Wind/Particle.hlsl"
#include "Assets/Compute/Wind/GridCell.hlsl"

uint max_grid_side_size;
float min_radius_sqr;
float max_radius_sqr;
float cell_coord_offset;
float cell_coord_mul;
float viscosity_coefficient;
float particle_influence_radius;
float energy_growth_factor;
float energy_loss_factor;
float zero_height_pressure;

static const float MolarAirMass = 0.0289644f; // kg/mol
static const float MolarGasConstant = 8.3144598f; // j/mol
static const float PressureCoefficient = MolarAirMass / MolarGasConstant;

float get_sqr_distance(int particle, float3 p)
{
    float3 d = particles[particle].position - p;
    return d.x * d.x + d.y * d.y + d.z * d.z;
}

float get_sqr_distance(int particleA, int particleB)
{
    float3 d = particles[particleA].position - particles[particleB].position;
    return d.x * d.x + d.y * d.y + d.z * d.z;
}

int index_from_cell(uint3 cell)
{
    return cell.x * max_grid_side_size * max_grid_side_size + cell.y * max_grid_side_size + cell.z;
}

float get_ejection_force(float height)
{
    return zero_height_pressure * exp(-9.81f * PressureCoefficient * height / 700);
}

uint3 cell_from_index(uint index)
{
    uint x = index / (max_grid_side_size * max_grid_side_size);
    uint r = index % (max_grid_side_size * max_grid_side_size);
    return uint3(x, r / max_grid_side_size, r % max_grid_side_size);
}

uint3 cell_from_coord(float3 coord)
{
    int cellsPerSideHalf = max_grid_side_size * 0.5f;
    return uint3(
        floor((coord.x) / cell_coord_mul + cellsPerSideHalf),
        floor((coord.y) / cell_coord_mul + cellsPerSideHalf),
        floor((coord.z) / cell_coord_mul + cellsPerSideHalf));
}

float3 cell_center_coord(uint3 cell)
{
    int cellsPerSideHalf = max_grid_side_size * 0.5f;
    return float3(
        ((float)cell.x - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset,
        ((float)cell.y - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset,
        ((float)cell.z - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset);
}


bool is_cell_valid(uint3 cell)
{
    float3 center = cell_center_coord(cell);
    float mag = center.x * center.x + center.y * center.y + center.z * center.z;
    return mag > min_radius_sqr && mag < max_radius_sqr;
}

#endif