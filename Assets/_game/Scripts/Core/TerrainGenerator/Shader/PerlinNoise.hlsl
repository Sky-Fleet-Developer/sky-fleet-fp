#ifndef PERLIN_NOISE
#define PERLIN_NOISE

// Gradient (Perlin) noise in two dimensions. It depends only on the position and the seed, so tiles computed one by one
// continue each other. Used by the noise biome source (NoiseBiomeTile.compute).

uint PerlinHash(int2 cell, uint seed)
{
    uint h = (uint)cell.x * 374761393u + (uint)cell.y * 668265263u + seed * 2246822519u;
    h = (h ^ (h >> 13)) * 1274126177u;
    return h ^ (h >> 16);
}

float2 PerlinGradient(int2 cell, uint seed)
{
    float angle = PerlinHash(cell, seed) * (6.2831853 / 4294967296.0);
    return float2(cos(angle), sin(angle));
}

// One octave, in about [-0.7071, 0.7071]
float PerlinNoise(float2 position, uint seed)
{
    int2 cell = (int2)floor(position);
    float2 local = position - cell;
    float2 fade = local * local * local * (local * (local * 6.0 - 15.0) + 10.0);

    float n00 = dot(PerlinGradient(cell, seed), local);
    float n10 = dot(PerlinGradient(cell + int2(1, 0), seed), local - float2(1, 0));
    float n01 = dot(PerlinGradient(cell + int2(0, 1), seed), local - float2(0, 1));
    float n11 = dot(PerlinGradient(cell + int2(1, 1), seed), local - float2(1, 1));
    return lerp(lerp(n00, n10, fade.x), lerp(n01, n11, fade.x), fade.y);
}

// Fractal sum of octaves, in 0..1. Every octave has its own seed, so their cells don't line up
float PerlinFbm(float2 position, uint seed, int octaves, float persistence, float lacunarity)
{
    float sum = 0;
    float amplitude = 1;
    float normalization = 0;
    float2 octave_position = position;
    for (int octave = 0; octave < octaves; octave++)
    {
        sum += amplitude * PerlinNoise(octave_position, seed + (uint)octave * 101u);
        normalization += amplitude;
        amplitude *= persistence;
        octave_position *= lacunarity;
    }
    return saturate(sum / max(normalization * 0.7071, 1e-5) * 0.5 + 0.5);
}

#endif
