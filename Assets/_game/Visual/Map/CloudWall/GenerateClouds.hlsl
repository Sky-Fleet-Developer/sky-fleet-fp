#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
uniform StructuredBuffer<float> rnd;

void GenerateClouds_float(float time, float2 uv, float frequency, float2 height_range, float width, float octaves, out float alpha)
{
    float r = 0;
    float ascending = 1; 
    float descending = 1;
    //float rnd[] = {1.1, 5.7, 6.15, 65.84, 8.15, 6.98, 1.258, 3.333, 2.987};
    for (int i = 0; i < octaves; i++)
    {
        float s = sin(time * rnd[i * 2] + uv.x * PI * frequency * ascending);
        r += /*(1 - abs(s * 2))*/ s * rnd[i * 2 + 1] * descending;
        ascending *= 2;
        descending *= 0.5;
    }
    alpha = max(1 - abs((r * height_range.y - (uv.y - height_range.x)) * width), 0);
    /*
    if (uv.y < height_range.x + (height_range.y - height_range.x) * (r - 1) * 0.5)
    {
        alpha = 1;
    }
    else
    {
        alpha = 0;
    }*/
}
#endif