#ifndef GRID
#define GRID

RWStructuredBuffer<particle> particles;
RWBuffer<int> grid;
RWBuffer<int> counter;
RWBuffer<int2> elements; //x is value, y is next
int grid_length;
int elements_length;
float delta_time;
float push_force;

void push_to_cell(int index, int particleIndex)
{
    int2 pointer;
    pointer.x = particleIndex;
    int indexWrite = 0;
    InterlockedAdd(counter[0], 1, indexWrite);
    int perviousHead = 0;
    InterlockedExchange(grid[index], indexWrite, perviousHead);
    pointer.y = perviousHead;
    
    elements[indexWrite] = pointer;
}


#endif