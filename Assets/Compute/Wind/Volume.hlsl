#ifndef VOLUME
#define VOLUME
#include "Assets/Compute/Wind/Particle.hlsl"
#include "Assets/Compute/Wind/GridCell.hlsl"

RWStructuredBuffer<particle> particles;
StructuredBuffer<int> grid_links; //линк[cell] - есть 

int max_grid_side_size;
int grid_links_count;
float min_radius_sqr;
float max_radius_sqr;
float cell_coord_offset;
float cell_coord_mul;
float particle_influence_radius;

float get_sqr_distance(int particleA, int particleB)
{
    float3 d = particles[particleA].position - particles[particleB].position;
    return d.x * d.x + d.y * d.y + d.z * d.z;
}

int index_from_cell(uint3 cell)
{
    return cell.x * max_grid_side_size * max_grid_side_size + cell.y * max_grid_side_size + cell.z;
}

uint3 cell_from_index(int index)
{
    int x = index / (max_grid_side_size * max_grid_side_size);
    int r = index % (max_grid_side_size * max_grid_side_size);
    return uint3(x, r / max_grid_side_size, r % max_grid_side_size);
}

uint3 cell_from_coord(float3 coord)
{
    int cellsPerSideHalf = max_grid_side_size / 2;
    return uint3(
        floor((coord.x) / cell_coord_mul + cellsPerSideHalf),
        floor((coord.y) / cell_coord_mul + cellsPerSideHalf),
        floor((coord.z) / cell_coord_mul + cellsPerSideHalf));
}

float3 cell_center_coord(uint3 cell)
{
    int cellsPerSideHalf = max_grid_side_size / 2;
    return float3(
        ((float)cell.x - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset,
        ((float)cell.y - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset,
        ((float)cell.z - cellsPerSideHalf) * cell_coord_mul + cell_coord_offset);
}

int cell_index_from_link(int link)
{
    return grid_links[link];
}

int link_from_index(int cellIndex)
{
    for (int i = 0; i < grid_links_count; i++)
    {
        if ((int)grid_links[i] == cellIndex)
        {
            return i;
        }
    }
    return -1;
}

int link_from_cell(uint3 cell)
{
    int index = index_from_cell(cell);
    return link_from_index(index);
}

bool is_cell_valid(uint3 cell)
{
    float3 center = cell_center_coord(cell);
    float mag = center.x * center.x + center.y * center.y + center.z * center.z;
    return mag > min_radius_sqr && mag < max_radius_sqr;
}

#endif