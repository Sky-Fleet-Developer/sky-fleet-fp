#ifndef PARTICLE
#define PARTICLE

struct particle
{
    float3 position;
    float3 velocity;
    float3 gradient;
    uint grid_index;
    float2 density;
};

/*struct packed_particle
{
    float position_x;
    float position_y;
    float position_z;
    float velocity_x;
    float velocity_y;
    float velocity_z;
    uint gradient_xy;
    uint gradient_z_and_grid_index;
};*/
/*uint maxHalf = 65535;
particle unpack_particle(packed_particle value)
{
    particle p;
    p.position = float3(value.position_x, value.position_y, value.position_z);
    p.velocity = float3(value.velocity_x, value.velocity_y, value.velocity_z);
    p.gradient.x = value.gradient_xy >> 16;
    p.gradient.y = value.gradient_xy;
    p.gradient.z = f16tof32(value.gradient_z_and_grid_index >> 16);
    p.grid_index = value.gradient_z_and_grid_index & maxHalf;
    return p;
}

packed_particle pack_particle(particle value)
{
    packed_particle p;
    p.position_x = value.position.x;
    p.position_y = value.position.y;
    p.position_z = value.position.z;
    p.velocity_x = value.velocity.x;
    p.velocity_y = value.velocity.y;
    p.velocity_z = value.velocity.z;
    p.gradient_xy = value.gradient.y | value.gradient.x << 16;
    p.gradient_z_and_grid_index = value.grid_index | f32tof16(value.gradient.z) << 16;
    return p;
}*/
/*
// Метод упаковки частицы
packed_particle pack_particle(particle p)
{
    // Создаем новую структуру для хранения упакованных данных
    packed_particle result;
    
    // Копируем позиции и скорости непосредственно
    result.position_x = p.position.x;
    result.position_y = p.position.y;
    result.position_z = p.position.z;
    result.velocity_x = p.velocity.x;
    result.velocity_y = p.velocity.y;
    result.velocity_z = p.velocity.z;
    
    // Объединяем компоненты градиента XY в один uint
    result.gradient_xy = (asuint(p.gradient.x) << 16) | asuint(p.gradient.y);
    
    // Складываем z-компоненту градиента и индекс сетки вместе
    result.gradient_z_and_grid_index = (asuint(p.gradient.z) << 16) | p.grid_index;
    
    return result;
}

// Метод распаковки обратно в полную структуру
particle unpack_particle(packed_particle pp)
{
    particle result;
    
    // Распаковываем позиции и скорости
    result.position = float3(pp.position_x, pp.position_y, pp.position_z);
    result.velocity = float3(pp.velocity_x, pp.velocity_y, pp.velocity_z);
    
    result.gradient.x = asfloat((pp.gradient_xy >> 16));     // Верхняя половина содержит x
    result.gradient.y = asfloat((pp.gradient_xy & 0xFFFF));  // Нижняя половина содержит y
    result.gradient.z = asfloat((pp.gradient_z_and_grid_index >> 16));   
    
    // Извлекаем сеточный индекс
    result.grid_index = pp.gradient_z_and_grid_index & 0xFF;
    
    return result;
}
*/
#endif