#ifndef STOCHASTIC_TILING
#define STOCHASTIC_TILING

// Stochastic tiling (Heitz & Neyret 2018, Mikkelsen 2022 "hex-tiling"): every repetition of a tiling texture is offset
// and rotated at random over a triangle grid, and the three tiles around a pixel are blended, so the tiling doesn't
// show. The caller samples its own textures for each of the three tiles:
//   StochasticTiles tiles = StochasticTilesSetup(uv, ddx(uv), ddy(uv), salt, rotation);
//   for i in 0..2: sample at StochasticTileUV(tiles, i, uv) with gradients StochasticTileGrad(tiles, i, tiles.gx / gy),
//                  rotate tangent space normals back with StochasticNormalBack(tiles, i, normal.xy)
//   float3 weights = StochasticTilesWeights(tiles, heights, contrast);
// Pixel shader only: the uv gradients decide the prefiltering.

struct StochasticTiles
{
    float2 cos_sin[3]; // rotation of each tile
    float2 offset[3];  // offset of each tile, in repetitions
    float3 weights;    // barycentric weights of the tiles
    float2 gx;         // uv gradients, widened by the prefiltering
    float2 gy;
    float coarse;      // 0..1: grid cells near pixel size, where the blend softens
};

uint StochasticHash(int2 cell, uint salt)
{
    // pcg2d
    uint2 v = asuint(cell) * 1664525u + 1013904223u + salt * 2654435769u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    return v.x ^ v.y;
}

float2 StochasticRotate(float2 v, float2 cos_sin)
{
    return float2(v.x * cos_sin.x - v.y * cos_sin.y, v.x * cos_sin.y + v.y * cos_sin.x);
}

float2 StochasticTileUV(StochasticTiles tiles, int i, float2 uv)
{
    return StochasticRotate(uv, tiles.cos_sin[i]) + tiles.offset[i];
}

float2 StochasticTileGrad(StochasticTiles tiles, int i, float2 gradient)
{
    return StochasticRotate(gradient, tiles.cos_sin[i]);
}

// A tangent space normal sampled in a tile, back in uv space
float2 StochasticNormalBack(StochasticTiles tiles, int i, float2 normal_xy)
{
    return StochasticRotate(normal_xy, float2(tiles.cos_sin[i].x, -tiles.cos_sin[i].y));
}

// The three tiles around uv (in repetitions of the textures). salt: different random tiles per texture or object;
// rotation 0..1: largest random rotation of a tile, in turns
StochasticTiles StochasticTilesSetup(float2 uv, float2 gx, float2 gy, uint salt, float rotation)
{
    StochasticTiles tiles;

    // Triangle grid: cells of about a third of a texture repetition (2 * sqrt(3) per repetition)
    const float cells_per_repetition = 3.46410162;
    float2 grid = uv * cells_per_repetition;

    // Prefiltering of the tile pattern itself. The mips filter each tile, but not the choice between tiles: once a grid
    // cell shrinks to a few pixels, neighbouring pixels land in different random tiles and the sharp blend between
    // them, and that aliases into noise. A pixel covering several cells sees the average of several random patches,
    // which is smoother than one patch of the pixel's size, so the footprint is widened to match (per axis, to keep
    // the anisotropic filtering), and the blend is softened to plain barycentric weights before that point.
    float2 footprint = float2(length(gx), length(gy)) * cells_per_repetition; // grid cells per pixel along ddx, ddy
    tiles.coarse = smoothstep(0.1, 0.5, max(footprint.x, footprint.y));
    float2 widen = max(1, footprint * 2);
    tiles.gx = gx * widen.x;
    tiles.gy = gy * widen.y;

    float2 skewed = float2(grid.x - 0.57735027 * grid.y, 1.15470054 * grid.y);
    int2 base = (int2)floor(skewed);
    float2 f = skewed - base;
    float rest = 1 - f.x - f.y;
    int2 vertices[3] =
    {
        rest > 0 ? base : base + int2(1, 1),
        base + (rest > 0 ? int2(0, 1) : int2(1, 0)),
        base + (rest > 0 ? int2(1, 0) : int2(0, 1)),
    };
    tiles.weights = rest > 0 ? float3(rest, f.y, f.x) : float3(-rest, 1 - f.y, 1 - f.x);

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        uint hash = StochasticHash(vertices[i], salt);
        tiles.offset[i] = float2(hash & 0xFFFF, hash >> 16) / 65536.0;
        float angle = StochasticHash(vertices[i], salt + 101u) * (6.28318530718 / 4294967296.0) * rotation;
        tiles.cos_sin[i] = float2(cos(angle), sin(angle));
    }
    return tiles;
}

// Final weights of the three tiles. heights: 0..1 per tile (0.5 without height maps); contrast 0..1: how much the
// heights decide the blend. Higher parts of a tile cover lower parts of its neighbours, then a sharp falloff keeps the
// tiles crisp. Both fade out where the cells get small on screen, where they would only alias
float3 StochasticTilesWeights(StochasticTiles tiles, float3 heights, float contrast)
{
    float3 weights = tiles.weights * lerp(1, heights + 0.05, contrast * (1 - tiles.coarse));
    weights = pow(max(weights, 0), lerp(7, 1, tiles.coarse));
    return weights / max(weights.x + weights.y + weights.z, 1e-6);
}

#endif
