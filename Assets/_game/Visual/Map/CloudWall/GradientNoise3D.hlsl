// Вспомогательная функция для сглаживания интерполяции (Smoothstep)
void evaluation_smoothstep(float3 x, out float3 t, out float3 dt)
{
    t = x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
    dt = 30.0 * x * x * (x * (x - 2.0) + 1.0);
}

// Хэш-функция для генерации псевдослучайных векторов направления
float3 hash3D(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}

// Главная функция для Shader Graph
void GradientNoise3D_float(float3 In, out float Out, out float3 Gradient)
{
    float3 p = floor(In);
    float3 f = frac(In);

    // Вычисление сглаживания и его производных
    float3 t, dt;
    evaluation_smoothstep(f, t, dt);

    // Координаты углов куба
    float3 g000 = hash3D(p + float3(0.0, 0.0, 0.0));
    float3 g100 = hash3D(p + float3(1.0, 0.0, 0.0));
    float3 g010 = hash3D(p + float3(0.0, 1.0, 0.0));
    float3 g110 = hash3D(p + float3(1.0, 1.0, 0.0));
    float3 g001 = hash3D(p + float3(0.0, 0.0, 1.0));
    float3 g101 = hash3D(p + float3(1.0, 0.0, 1.0));
    float3 g011 = hash3D(p + float3(0.0, 1.0, 1.0));
    float3 g111 = hash3D(p + float3(1.0, 1.0, 1.0));

    // Проекции на векторы смещения
    float v000 = dot(g000, f - float3(0.0, 0.0, 0.0));
    float v100 = dot(g100, f - float3(1.0, 0.0, 0.0));
    float v010 = dot(g010, f - float3(0.0, 1.0, 0.0));
    float v110 = dot(g110, f - float3(1.0, 1.0, 0.0));
    float v001 = dot(g001, f - float3(0.0, 0.0, 1.0));
    float v101 = dot(g101, f - float3(1.0, 0.0, 1.0));
    float v011 = dot(g011, f - float3(0.0, 1.0, 1.0));
    float v111 = dot(g111, f - float3(1.0, 1.0, 1.0));

    // Интерполяция по оси X
    float a = v000 + t.x * (v100 - v000);
    float b = v010 + t.x * (v110 - v010);
    float c = v001 + t.x * (v101 - v001);
    float d = v011 + t.x * (v111 - v011);

    // Производные по оси X
    float da = g000.x + t.x * (g100.x - g000.x) + dt.x * (v100 - v000);
    float db = g010.x + t.x * (g110.x - g010.x) + dt.x * (v110 - v010);
    float dc = g001.x + t.x * (g101.x - g001.x) + dt.x * (v101 - v001);
    float dd = g011.x + t.x * (g111.x - g011.x) + dt.x * (v111 - v011);

    // Интерполяция по оси Y
    float e = a + t.y * (b - a);
    float h = c + t.y * (d - c);

    // Производные по оси Y
    float de_dy = (g010.y - g000.y) + t.x * (g110.y - g100.y - g010.y + g000.y) + dt.y * (b - a);
    float dh_dy = (g011.y - g001.y) + t.x * (g111.y - g101.y - g011.y + g001.y) + dt.y * (d - c);

    // Финальное значение шума (переведено в диапазон от 0 до 1)
    float noiseValue = e + t.z * (h - e);
    Out = noiseValue * 0.5 + 0.5;

    // Вычисление финального вектора градиента
    Gradient.x = da + t.y * (db - da) + t.z * (dc - da + t.y * (dd - dc - db + da));
    Gradient.y = de_dy + t.z * (dh_dy - de_dy);
    Gradient.z = (g001.z - g000.z) + t.x * (g101.z - g100.z - g001.z + g000.z) + t.y * (g011.z - g010.z - g101.z + g100.z) + dt.z * (h - e);
}
