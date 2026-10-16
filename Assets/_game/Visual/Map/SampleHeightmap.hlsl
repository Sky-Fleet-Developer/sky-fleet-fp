// Geomorphing of the patches, toggled by QuadTreeLodConfig.Geomorphing. Read by the shader graph through
// "Use Pragmas" of the custom function node
#pragma multi_compile_local_vertex _ TERRAIN_GEOMORPH

#include "Heightmap.hlsl"
#include "SampleMapUtility.hlsl"
#include "TerrainView.hlsl"
#include "TerrainLodMap.hlsl"

float2 hash_p(float2 p) { 
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))); 
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123); 
}

// Базовый 2D шум Перлина
float perlin_noise(float2 uv) { 
    float2 i = floor(uv); 
    float2 f = frac(uv); 

    // Сглаживание по кривой Эрмита (Quintic interpolation) 
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); 

    // Градиенты в четырех углах ячейки (корректные смещения)
    float dotBottomLeft  = dot(hash_p(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)); 
    float dotBottomRight = dot(hash_p(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)); 
    float dotTopLeft     = dot(hash_p(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)); 
    float dotTopRight    = dot(hash_p(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)); 

    // Корректная интерполяция: сначала по X (низ и верх), затем по Y между ними
    return lerp( 
        lerp(dotBottomLeft, dotBottomRight, u.x), 
        lerp(dotTopLeft, dotTopRight, u.x), 
        u.y 
    ); 
}

// Генерация шума с заданным количеством октав (FBM)
float fbm_perlin(float2 uv, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = 1.0;
    float maxValue = 0.0; // Используется для нормализации результата в диапазон [-1, 1] или [0, 1]

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * perlin_noise(uv * frequency);
        maxValue += amplitude;
        frequency *= lacunarity;
        amplitude *= gain;
    }

    // Возвращает шум в диапазоне [0.0, 1.0]
    return (value / maxValue) * 0.5 + 0.5;
}

void SampleHeightmap(float3 world_position, out float height)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);
    //world_position.x = (world_position.x - position_to_chunk_matrix.z) * position_to_chunk_matrix.x;
    //world_position.z = (world_position.z - position_to_chunk_matrix.w) * position_to_chunk_matrix.y;
    //height = _FadeTargetRange.x + fbm_perlin(world_position.xz * _ErosionUvScale, 3) * _FadeTargetRange.y;//
    float width, height_px, elements;
    source_heightmap.GetDimensions(width, height_px, elements);
    height = source_heightmap.SampleLevel(sampler_source_heightmap, float3(ChunkUvToTextureUv(uv, width, heightmap_shared_edges > 0.5), chunk), 0);
}

// Blended biome debug colors, same layers and pixel layout as source_heightmap. Bound by TerrainProvider.
Texture2DArray<float4> biome_debug_map;
SamplerState sampler_biome_debug_map;
float biome_debug_enabled;

float3 SampleBiomeDebugColor(float3 world_position)
{
    if (biome_debug_enabled < 0.5)
    {
        return float3(1, 1, 1);
    }

    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    int chunk = SlotToMap(slot);
    float width, height_px, elements;
    biome_debug_map.GetDimensions(width, height_px, elements);
    return biome_debug_map.SampleLevel(sampler_biome_debug_map, float3(ChunkUvToTextureUv(uv, width, heightmap_shared_edges > 0.5), chunk), 0).rgb;
}

Texture2D<float4> terrain_height_gradient;
SamplerState sampler_terrain_height_gradient;
float2 terrain_height_view_range; // normalized heights mapped to the ends of the gradient
// Grid of map_size x map_size cells, cell (x, y) shows texture slot (x, y)
Texture2D<float4> terrain_slot_texture;
SamplerState sampler_terrain_slot_texture;

float3 SampleHeightViewColor(float normalized_height)
{
    float t = saturate((normalized_height - terrain_height_view_range.x) / max(terrain_height_view_range.y - terrain_height_view_range.x, 1e-5));
    return terrain_height_gradient.SampleLevel(sampler_terrain_height_gradient, float2(t, 0.5), 0).rgb;
}

float3 SampleSlotViewColor(float3 world_position)
{
    int slot;
    float2 uv;
    WorldPositionToSlot(world_position, uv, slot);
    // Texture array layer = slot.x * map_size + slot.y, see TerrainData.SlotToLayer
    int layer = SlotToMap(slot);
    uint size = (uint)map_size;
    float2 slot_coord = float2((uint)layer / size, (uint)layer % size);
    return terrain_slot_texture.SampleLevel(sampler_terrain_slot_texture, (slot_coord + uv) / map_size, 0).rgb;
}

float3 SampleViewModeColor(float3 world_position, float normalized_height)
{
    int mode = TerrainViewMode();
    float3 color = float3(1, 1, 1);
    if (mode == 1)
    {
        color = SampleBiomeDebugColor(world_position);
    }
    else if (mode == 2)
    {
        color = SampleHeightViewColor(normalized_height);
    }
    else if (mode == 3)
    {
        color = SampleSlotViewColor(world_position);
    }
    return color;
}

// Distance between two neighbouring heightmap pixels in world units
float HeightmapPixelSize()
{
    float width, height_px, elements;
    source_heightmap.GetDimensions(width, height_px, elements);
    float pixels_per_chunk = heightmap_shared_edges > 0.5 ? width - 1 : width;
    // position_to_chunk_matrix.x = 1 / heightmap chunk size in meters
    return 1.0 / (position_to_chunk_matrix.x * pixels_per_chunk);
}

// Normalized height and the normal of the heightmap at a point
void SampleHeightAndNormal(float3 world_position, out float height, out float3 normal)
{
    SampleHeightmap(world_position, height);

    // Central difference over one pixel on each side. Bilinear filtering makes the height linear inside a pixel,
    // so a smaller step returns the constant slope of a single pixel (faceted normals), while this step
    // returns the bilinear blend of the per-pixel slopes (smooth normals)
    float step = HeightmapPixelSize();
    float height_x_pos, height_z_pos, height_x_neg, height_z_neg;
    SampleHeightmap(world_position + float3(step, 0, 0), height_x_pos);
    SampleHeightmap(world_position + float3(0, 0, step), height_z_pos);

    SampleHeightmap(world_position + float3(-step, 0, 0), height_x_neg);
    SampleHeightmap(world_position + float3(0, 0, -step), height_z_neg);

    float dfdx = (height_x_pos - height_x_neg);
    float dfdz = (height_z_pos - height_z_neg);

    // Erosion and other heightmap filters are already baked into source_heightmap
    float2 gradient = float2(-dfdx, -dfdz);

    // Normalized height 1.0 equals height_scale meters
    float2 world_slope = gradient * (height_scale / (2.0 * step));
    normal = normalize(float3(world_slope.x, 1.0, world_slope.y));
}

// Height and normal of the vertex at index (see TerrainVertexIndex) of a patch of patch_size cells. With geomorphing
// they blend towards the ones the parent patch has there: the vertices off the parent grid lie on its straight edges,
// between two of its vertices. The triangles of Patch.CreateMesh are split along the (+1, -1) diagonal, so the
// vertices in the middle of a parent quad lie on that diagonal
void SamplePatchVertex(int2 index, float patch_size, out float height, out float3 normal)
{
    SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index)), height, normal);
#if defined(TERRAIN_GEOMORPH)
    float morph = TerrainMorph(TerrainVertexPosition(index), patch_size);
    int step = max((int)patch_size, 1); // between the vertices of the patch
    int2 odd = int2(FloorDiv(index.x, step), FloorDiv(index.y, step)) & 1;
    if (morph > 0 && any(odd))
    {
        int2 offset = odd.x && odd.y ? int2(step, -step) : (odd.x ? int2(step, 0) : int2(0, step));
        float height_a, height_b;
        float3 normal_a, normal_b;
        SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index + offset)), height_a, normal_a);
        SampleHeightAndNormal(TerrainToWorldPosition(TerrainVertexPosition(index - offset)), height_b, normal_b);
        height = lerp(height, (height_a + height_b) * 0.5, morph);
        normal = normalize(lerp(normal, normalize(normal_a + normal_b), morph));
    }
#endif
}

// Vertex of the terrain patches: height, normal and the object space position to draw it at. Everything is computed
// from the integer vertex index, so a vertex shared by two patches comes out bit-identical from both, and on a border
// with a coarser patch the vertex collapses onto a vertex of that patch (see TerrainStitch): the patches are watertight.
// object_position: the mesh vertex, in the space of the patch transform (scaled by the patch size in cells)
void SampleHeightmapWithGradient_float(float3 world_position, float3 object_position,
    out float height, out float3 normal, out float3 debug_color, out float3 position_os)
{
    int2 index = TerrainVertexIndex(TerrainSpacePosition(world_position));
    float patch_size;
    int2 target;
    TerrainStitch(index, patch_size, target);
    SamplePatchVertex(target, patch_size, height, normal);

    debug_color = SampleViewModeColor(TerrainToWorldPosition(TerrainVertexPosition(target)), height);
    height *= height_scale;

    // The mesh vertices lie on the same grid in object space, one unit per vertex whatever the patch size: the exact
    // vertex is restored by rounding, then moved by the collapse, which is a whole number of this patch's vertices.
    // The mesh is flat at y = 0
    int own_size = max((int)round(GetObjectToWorldMatrix()._m00), 1);
    int2 mesh_index = (int2)round(object_position.xz / TerrainVertexStep()) + (target - index) / own_size;
    float2 mesh_xz = mesh_index * TerrainVertexStep();
    position_os = float3(mesh_xz.x, height, mesh_xz.y);
}
