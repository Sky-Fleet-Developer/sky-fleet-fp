// Для использования StructuredBuffer в Shader Graph HDRP
#if defined(SHADERGRAPH_PREVIEW)
    // В превью буфер недоступен
#else
    StructuredBuffer<float> random_numbers;
#endif

void CloudRaymarch_float(
    float3 WorldPos,          // Absolute World Position пикселя
    float3 CameraPos,         // Позиция камеры в мире
    float3 ObjectScale,       // Масштаб объекта (передается из скрипта/свойств)
    float4x4 WorldToLocal,    // Матрица трансформации мира в локальные координаты тора
    float4x4 LocalToWorld,
    float R,                  // Главный радиус тора
    float r,                  // Радиус сечения (толщина облака)
    int MaxSteps,             // Количество шагов (например, 32-64)
    float StepSize,           // Длина одного шага
    float DensityMultiplier,  // Множитель плотности облака
    out float4 OutColor       // Итоговый цвет (RGB + Alpha)
)
{
    float4 localCam4 = mul(WorldToLocal, float4(CameraPos, 1.0));
    float4 localPos4 = mul(WorldToLocal, float4(WorldPos, 1.0));
    
    float3 localCam = localCam4.xyz / localCam4.w;
    float3 localPos = localPos4.xyz / localPos4.w;

    localCam /= ObjectScale;
    localPos /= ObjectScale;

    float3 localDir = normalize(localPos - localCam);

    float accumulatedDensity = 0.0;
    float t = 0.0;
    bool hitTorus = false;

    // 3. Быстрый поиск пересечения (Реймарчинг для поиска оболочки тоer)
    [loop]
    for (int i = 0; i < MaxSteps; i++)
    {
        float3 p = localPos + localDir * t;
        
        // Функция расстояния до тора
        float2 q = float2(length(p.xz) - R, p.y);
        float d = length(q) - r;

        // Если мы вошли внутрь тора
        if (d < 0.001)
        {
            hitTorus = true;
            break;
        }
        
        t += d; // Делаем безопасный шаг на расстояние SDF
        
        // Ограничение, если улетели слишком далеко
        if (t > 1000.0) break;
    }

    // 4. Если луч промахнулся мимо тора, возвращаем прозрачность
    if (!hitTorus)
    {
        OutColor = float4(0, 0, 0, 0);
        return;
    }
    float t_collision = t;
    // 5. Итерация внутри тора для накопления плотности облака
    [loop]
    for (int j = 0; j < MaxSteps; j++)
    {
        float3 p = localPos + localDir * t;
        
        // Повторно считаем SDF
        float2 q = float2(length(p.xz) - R, p.y);
        float d = length(q) - r;

        // Если вышли из тора — прекращаем вычисления
        if (d > 0.001) break;

        // Шум и искажения
        float noise = 0.0;
        //#if !defined(SHADERGRAPH_PREVIEW)
        //    // Пример чтения из StructuredBuffer по индексу (зависит от логики вашего буфера)
        //    // Здесь используем псевдослучайный индекс на основе позиции
        //    uint rIndex = uint(abs(sin(dot(p, float3(12.9898, 78.233, 45.164)))) * 10000.0) % 1024;
        //    noise = random_numbers[rIndex] * 0.2; 
        //#endif

        // Плотность зависит от глубины погружения в тор (-d показывает глубину, так как d отрицательный)
        float currentDensity = (-d + noise) * DensityMultiplier;
        currentDensity = max(0.0, currentDensity);

        // Накапливаем непрозрачность
        accumulatedDensity += currentDensity * StepSize;

        // Оптимизация: если облако уже полностью непрозрачное, выходим
        if (accumulatedDensity >= 1.0)
        {
            accumulatedDensity = 1.0;
            break;
        }

        t += StepSize; // Внутри тора идем с фиксированным шагом
    }
    //float3 p = localPos + localDir * t_collision;
    //float3 c = normalize(float3(p.x, 0, p.z)) * R;
    //float3 normal = normalize(mul((float3x3)LocalToWorld, normalize(p - c)));
    //float lightness = (1 + dot(normal, float3(0, 1, 0))) * 0.5;
    // Формируем финальный белый цвет облака с накопленной альфой
    //OutColor = float4((float3(1,1,1) + normal) * 0.5f, accumulatedDensity);
    OutColor = float4(1,1,1, accumulatedDensity);

}
