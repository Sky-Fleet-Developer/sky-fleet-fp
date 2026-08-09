void CloudRaymarch_float(
UnityTexture2D height_map, 
UnitySamplerState _sampler, 
float2 uv, 
float4 height_map_st,
float3 view_dir_ts, 
float steps,      
float scale,    // Scale теперь регулирует общую силу эффекта (интенсивность параллакса)
float depth,    // Физическая глубина объема
out float alpha
)
{
    // 1. Считаем длину одного шага по глубине
    float step_length = depth / steps;
    
    // 2. ВАЖНО: Считаем смещение UV НА ЕДИНИЦУ ГЛУБИНЫ. 
    // Умножаем на scale и тайлинг, делим на view_dir_ts.z для компенсации угла.
    float2 uv_dir = (view_dir_ts.xy * height_map_st.xy * scale) / view_dir_ts.z;
    
    // 3. Шаг смещения UV строго привязан к физической длине шага depth
    float2 deltaUV = uv_dir * step_length;
    
    // Применяем начальный Tiling и Offset к UV
    float2 currentUV = uv * height_map_st.xy + height_map_st.zw + deltaUV * steps * 0.5;
    
    float current_depth = -0.01;
    float total_density = 0.0;

    for (int i = 0; i < (int)steps; i++)
    {
        float4 c = height_map.Sample(_sampler, currentUV);
        
        // Твоя логика: инвертируем, чтобы белые зоны шума были "выше" (ближе к началу)
        float cloud_start_depth = (1.0 - c.r * c.a) * depth; 

        if (current_depth > cloud_start_depth)
        {
            total_density += step_length;
        }
        
        // Смещаем UV и глубину на синхронную величину
        currentUV -= deltaUV;
        current_depth += step_length;
    }

    // Нормализуем альфу, разделив накопленную плотность на общую глубину
    alpha = saturate(total_density / depth);
}

