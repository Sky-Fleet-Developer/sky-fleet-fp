#ifndef SCATTER_PLACEMENT
#define SCATTER_PLACEMENT

// Placement of scattered details (grass, small stones) on the GPU, shared by the placement shader of every kind.
// Driven by Runtime.Scatter.ScatterPipeline, every frame:
//   Reset      clears the counters
//   CullTiles  one thread per tile around the camera: range, frustum and terrain occlusion; visible tiles are listed
//   Place      (the kind's own) one group per visible tile, a loop over the candidates of the tile: ScatterTryCandidate
//              picks the layer and finds the root, the kind builds its instance and stores it with ScatterAllocate
//   ClampArgs  turns the instance counts into the index counts of the draws, within the parts of the instance buffer
// Instances are drawn in batches, each with a part of the instance buffer and a mesh holding its shape once for every
// instance of that part. The kind declares the instance buffer: RWStructuredBuffer<its instance> scatter_instances.
// A kind's placement shader declares the four kernels and #pragma multi_compile_local _ TERRAIN_GEOMORPH.
// Positions: terrain space (TerrainSpacePosition) for the stable placement grid, absolute world for everything drawn.
// Random salts per candidate (ScatterRandom): 1, 2 and 15 are taken here, the kinds use 3 and up.

#include "Assets/_game/Visual/Map/TerrainHeight.hlsl"
#include "Assets/_game/Visual/Map/TerrainSurfaceWeights.hlsl"
#include "ScatterCommon.hlsl"

#define SCATTER_PLACE_THREADS 64

// Matches GraphicsBuffer.IndirectDrawIndexedArgs
struct ScatterDrawArgs
{
    uint index_count;
    uint instance_count;
    uint start_index;
    uint base_vertex;
    uint start_instance;
};

StructuredBuffer<ScatterLayer> scatter_layers;        // grouped by surface type
StructuredBuffer<uint2> scatter_surface_layers; // by surface id: first layer, layer count
uint scatter_surface_count;
float scatter_max_density; // details per square meter of the densest surface, all its layers together

float3 scatter_camera; // absolute world
float4 scatter_frustum[6]; // planes, inside where dot(xyz, p) + w >= 0
float scatter_range; // meters
float scatter_fade; // part of the range over which the details fade out
float scatter_full_density_distance;
float scatter_detailed_distance; // details closer use the near batches
float scatter_max_height; // tallest detail, for the tile bounds
float scatter_tile_size;
int scatter_tiles_side;
float scatter_occlusion; // 1: skip tiles behind the terrain
int scatter_occlusion_steps;
float scatter_shadows; // 1: near details cast shadows, so they are kept outside the view too

StructuredBuffer<ScatterBatch> scatter_batches;
uint scatter_batch_count;
StructuredBuffer<ScatterArea> scatter_areas;
uint scatter_area_count;
StructuredBuffer<float2> scatter_polygons; // points of the polygon areas

// Cover (Core.Scatter.IScatterGpuCover): ground covered by things placed earlier this frame, on grids of cells around
// the camera, one per layer (0 large objects, 1 small details), one after the other in the buffer. A cell holds the
// stamp of the frame (high 24 bits) and the cover 0..255 (low 8 bits), so cells of earlier frames read as uncovered
// and the grids are never cleared
RWStructuredBuffer<uint> scatter_cover;
float2 scatter_cover_origin; // absolute world x, z of the corner of cell 0
float scatter_cover_cell;    // meters
int scatter_cover_size;      // cells along each side
uint scatter_cover_stamp;
uint scatter_cover_read;     // layers read, bit per layer: candidates shrink where covered
int scatter_cover_write;     // layer written by ScatterCoverWrite, -1 none

RWStructuredBuffer<uint> scatter_dispatch_args; // thread groups of Place
// One per batch, each drawn once: instance_count counts the instances while they are placed, ClampArgs turns it into
// the index count
RWStructuredBuffer<ScatterDrawArgs> scatter_draw_args;
RWStructuredBuffer<int4> scatter_visible_tiles; // xy tile, z nearest distance (asint)

float ScatterRandom(int2 tile, uint index, uint salt)
{
    return ScatterHash(uint3(asuint(tile.x), asuint(tile.y), index * 16u + salt)) * (1.0 / 4294967296.0);
}

// A layer of a surface: random per candidate, independent from layer to layer
float ScatterLayerRandom(int2 tile, uint index, uint layer)
{
    return ScatterHash(uint3(asuint(tile.x), asuint(tile.y) ^ ((layer + 1) * 0x9E3779B9u), index * 16u + 2)) *
        (1.0 / 4294967296.0);
}

// One of count items (count > 0), by a random value 0..1
uint ScatterPickIndex(float random, uint count)
{
    return min((uint)(random * count), count - 1);
}

float ScatterLattice(int2 cell, uint seed)
{
    return ScatterHash(uint3(asuint(cell.x), asuint(cell.y), seed * 977u + 12345u)) * (1.0 / 4294967296.0);
}

// 0..1, smooth between random values at whole coordinates
float ScatterValueNoise(float2 p, uint seed)
{
    float2 cell = floor(p);
    float2 f = p - cell;
    float2 u = f * f * f * (f * (f * 6 - 15) + 10);
    int2 i = (int2)cell;
    float a = ScatterLattice(i, seed);
    float b = ScatterLattice(i + int2(1, 0), seed);
    float c = ScatterLattice(i + int2(0, 1), seed);
    float d = ScatterLattice(i + int2(1, 1), seed);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Share of the density of a layer kept at a point of the terrain: its patches.
// The edges of the patches come from the distribution of this noise (Runtime.Scatter.ScatterLayerTable)
float ScatterLayerNoise(ScatterLayer layer, float2 terrain_xz)
{
    if (layer.noise.x <= 0)
    {
        return 1;
    }
    float2 p = terrain_xz * layer.noise.x;
    float n = (ScatterValueNoise(p, layer.noise_seed * 2) + 0.5 * ScatterValueNoise(p * 2.03 + 0.37, layer.noise_seed * 2 + 1)) / 1.5;
    n = layer.noise.w > 0.5 ? 1 - n : n;
    return smoothstep(layer.noise.y, layer.noise.z, n);
}

// Height of the heightmap above sea level of the scene, in meters
float TerrainWorldHeight(float3 world_position)
{
    float height;
    SampleHeightmap(world_position, height);
    return height * height_scale + terrain_origin.y;
}

// Share of the full density kept at a distance: full near the camera, then falling with the square of the distance,
// so the details per pixel stay about the same
float ScatterDensityFactor(float distance)
{
    float ratio = scatter_full_density_distance / max(distance, 1e-3);
    return min(1, ratio * ratio);
}

int2 ScatterFirstTile()
{
    float2 camera = TerrainSpacePosition(scatter_camera);
    return (int2)floor(camera / scatter_tile_size) - (scatter_tiles_side >> 1);
}

bool ScatterInFrustum(float3 box_min, float3 box_max)
{
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float4 plane = scatter_frustum[i];
        float3 farthest = float3(plane.x >= 0 ? box_max.x : box_min.x, plane.y >= 0 ? box_max.y : box_min.y,
            plane.z >= 0 ? box_max.z : box_min.z);
        if (dot(plane.xyz, farthest) + plane.w < 0)
        {
            return false;
        }
    }
    return true;
}

// A sphere is at least partly inside the frustum
bool ScatterSphereInFrustum(float3 center, float radius)
{
    bool inside = true;
    [unroll]
    for (int p = 0; p < 6; p++)
    {
        inside = inside && dot(scatter_frustum[p].xyz, center) + scatter_frustum[p].w >= -radius;
    }
    return inside;
}

// The terrain doesn't rise above the line from the camera to the target
bool ScatterInSight(float3 target)
{
    // A little slack: the mesh and the bilinear heightmap differ slightly
    const float margin = 0.5;
    for (int i = 1; i < scatter_occlusion_steps; i++)
    {
        float3 p = lerp(scatter_camera, target, (float)i / scatter_occlusion_steps);
        if (TerrainWorldHeight(p) > p.y + margin)
        {
            return false;
        }
    }
    return true;
}

// Signed distance to a polygon in the xz plane: negative inside
float ScatterPolygonDistance(float2 p, uint start, uint count)
{
    float2 first = scatter_polygons[start];
    float distance_squared = dot(p - first, p - first);
    float inside = 1;
    uint previous = count - 1;
    for (uint i = 0; i < count; i++)
    {
        float2 a = scatter_polygons[start + i];
        float2 b = scatter_polygons[start + previous];
        float2 edge = b - a;
        float2 to_point = p - a;
        float2 off_edge = to_point - edge * saturate(dot(to_point, edge) / max(dot(edge, edge), 1e-12));
        distance_squared = min(distance_squared, dot(off_edge, off_edge));
        // Every edge crossed by a ray along +x flips inside and outside
        bool3 crossing = bool3(p.y >= a.y, p.y < b.y, edge.x * to_point.y > edge.y * to_point.x);
        if (all(crossing) || !any(crossing))
        {
            inside = -inside;
        }
        previous = i;
    }
    return inside * sqrt(distance_squared);
}

// 0 inside an area kept clear, growing back to 1 over the edge of the area
float ScatterAreaScale(float3 root)
{
    float scale = 1;
    for (uint a = 0; a < scatter_area_count; a++)
    {
        ScatterArea area = scatter_areas[a];
        float3 p = mul(area.world_to_local, float4(root, 1)).xyz;
        float distance;
        if (area.shape == 0)
        {
            float3 q = abs(p) - area.size * 0.5;
            distance = length(max(q, 0)) + min(max(q.x, max(q.y, q.z)), 0);
        }
        else if (area.shape == 1)
        {
            distance = length(p) - area.size.x;
        }
        else
        {
            float vertical = max(area.size.y - p.y, p.y - area.size.z);
            distance = max(ScatterPolygonDistance(p.xz, area.polygon_start, area.polygon_count), vertical);
        }
        scale = min(scale, area.edge > 0 ? saturate(distance / area.edge) : (distance > 0 ? 1 : 0));
    }
    return scale;
}

uint ScatterCoverIndex(uint layer, int2 cell)
{
    return (layer * scatter_cover_size + cell.y) * scatter_cover_size + cell.x;
}

// The strongest cover of the read layers at a cell
float ScatterCoverCell(int2 cell)
{
    if (any(cell < 0) || any(cell >= scatter_cover_size))
    {
        return 0;
    }
    float cover = 0;
    [unroll]
    for (uint layer = 0; layer < 2; layer++)
    {
        if (scatter_cover_read & (1u << layer))
        {
            uint value = scatter_cover[ScatterCoverIndex(layer, cell)];
            cover = max(cover, (value >> 8) == scatter_cover_stamp ? (value & 255) / 255.0 : 0);
        }
    }
    return cover;
}

// 0..1, bilinear between the cells
float ScatterCoverAt(float2 world_xz)
{
    float2 p = (world_xz - scatter_cover_origin) / scatter_cover_cell - 0.5;
    int2 cell = (int2)floor(p);
    float2 f = p - cell;
    return lerp(lerp(ScatterCoverCell(cell), ScatterCoverCell(cell + int2(1, 0)), f.x),
        lerp(ScatterCoverCell(cell + int2(0, 1)), ScatterCoverCell(cell + int2(1, 1)), f.x), f.y);
}

// Covers one cell of the write layer by a disc around grid position p (in cells), its edge softened over one cell
void ScatterCoverWriteCell(int2 cell, float2 p, float radius)
{
    if (any(cell < 0) || any(cell >= scatter_cover_size))
    {
        return;
    }
    float distance_to_center = length(cell + 0.5 - p) * scatter_cover_cell;
    float cover = saturate((radius - distance_to_center) / scatter_cover_cell + 0.5);
    if (cover > 0)
    {
        InterlockedMax(scatter_cover[ScatterCoverIndex((uint)scatter_cover_write, cell)],
            (scatter_cover_stamp << 8) | (uint)round(cover * 255));
    }
}

// Grid position (in cells) of an absolute world point
float2 ScatterCoverPosition(float2 world_xz)
{
    return (world_xz - scatter_cover_origin) / scatter_cover_cell;
}

// Covers a disc of the ground for the kinds placed later this frame, by one thread: for small discs (up to 24 cells
// across each way). Discs smaller than a cell cover only the cells whose centers they reach
void ScatterCoverWrite(float2 world_xz, float radius)
{
    if (radius <= 0 || scatter_cover_write < 0)
    {
        return;
    }
    float2 p = ScatterCoverPosition(world_xz);
    int2 center = (int2)floor(p);
    int reach = min((int)ceil(radius / scatter_cover_cell), 24);
    for (int y = -reach; y <= reach; y++)
    {
        for (int x = -reach; x <= reach; x++)
        {
            ScatterCoverWriteCell(center + int2(x, y), p, radius);
        }
    }
}

// A visible tile and its candidates
struct ScatterTile
{
    int2 tile;
    float full_count; // candidates at full density
    uint count;       // candidates at the density of its nearest point
    float2 seed;
};

// A candidate that grows: its layer and where
struct ScatterCandidate
{
    uint layer;         // index into scatter_layers and the layers of the kind
    float2 terrain_xz;
    float3 world_flat;  // absolute world x, z (y is not the terrain height)
    float3 root;        // absolute world, on the terrain mesh
    float distance;     // from the camera to the root
    float scale;        // 0..1: growing in with the density, fading out with the range, shrinking at the areas
    bool near;          // uses the near batches
};

// Candidates of a visible tile at full density. The candidates follow a low discrepancy sequence, so every prefix of
// it covers the tile evenly: thinning with distance keeps the first ones, and the details stay put as it changes
ScatterTile ScatterLoadTile(uint visible_index)
{
    int4 visible = scatter_visible_tiles[visible_index];
    ScatterTile t;
    t.tile = visible.xy;
    t.full_count = scatter_tile_size * scatter_tile_size * scatter_max_density;
    t.count = (uint)ceil(t.full_count * ScatterDensityFactor(asfloat(visible.z)));
    t.seed = float2(ScatterRandom(t.tile, 0, 15), ScatterRandom(t.tile, 1, 15));
    return t;
}

// Candidate i of a tile: false when nothing grows there. Otherwise picks one of the layers of the surfaces under it,
// each by the weight of its surface, its density and its patches, by its share of the total (weighted reservoir
// sampling); the candidate grows with the total's share of the densest surface
bool ScatterTryCandidate(ScatterTile t, uint i, out ScatterCandidate c)
{
    c = (ScatterCandidate)0;
    float2 jitter = frac(t.seed + (i + 1) * float2(0.7548776662, 0.5698402910));
    c.terrain_xz = (t.tile + jitter) * scatter_tile_size;
    c.world_flat = TerrainToWorldPosition(c.terrain_xz);
    // The 3D distance is at least the horizontal one: a cheap first rejection
    float flat_distance = distance(c.world_flat.xz, scatter_camera.xz);
    if ((float)i >= t.full_count * ScatterDensityFactor(flat_distance) + 1 || flat_distance > scatter_range)
    {
        return false;
    }

    CollectTerrainSurfaces(c.world_flat);
    float total = 0;
    for (int s = 0; s < surface_candidate_count; s++)
    {
        uint id = (uint)surface_candidate_id[s];
        if (id >= scatter_surface_count)
        {
            continue;
        }
        uint2 layers = scatter_surface_layers[id];
        for (uint l = layers.x; l < layers.x + layers.y; l++)
        {
            ScatterLayer layer = scatter_layers[l];
            float weight = surface_candidate_weight[s];
            if (weight < layer.min_weight)
            {
                continue;
            }
            float share = layer.density * weight * ScatterLayerNoise(layer, c.terrain_xz);
            if (share > 0)
            {
                total += share;
                if (ScatterLayerRandom(t.tile, i, l) * total < share)
                {
                    c.layer = l;
                }
            }
        }
    }
    if (ScatterRandom(t.tile, i, 1) * scatter_max_density >= total)
    {
        return false;
    }

    c.root = float3(c.world_flat.x, TerrainMeshHeight(c.terrain_xz) + terrain_origin.y, c.world_flat.z);
    c.distance = distance(c.root, scatter_camera);
    // Past its share of the density the candidate grows in over one rank, so thinning doesn't pop
    float grow = saturate(t.full_count * ScatterDensityFactor(c.distance) - i);
    float fade = 1 - smoothstep(scatter_range * (1 - scatter_fade), scatter_range, c.distance);
    c.scale = grow * fade;
    if (c.scale <= 0.01)
    {
        return false;
    }
    c.scale *= ScatterAreaScale(c.root);
    if (c.scale <= 0.01)
    {
        return false;
    }
    if (scatter_cover_read != 0)
    {
        c.scale *= 1 - ScatterCoverAt(c.root.xz);
        if (c.scale <= 0.01)
        {
            return false;
        }
    }

    c.near = c.distance < scatter_detailed_distance;
    return true;
}

// The instance is kept outside the view when it may cast a shadow into it
bool ScatterKeepOutsideView(ScatterCandidate c)
{
    return c.near && scatter_shadows > 0.5;
}

// A slot of the instance buffer in a batch; false when the batch is full
bool ScatterAllocate(uint batch, out uint slot)
{
    batch = min(batch, scatter_batch_count - 1);
    uint index;
    InterlockedAdd(scatter_draw_args[batch].instance_count, 1, index);
    ScatterBatch part = scatter_batches[batch];
    slot = part.offset + index;
    return index < part.capacity;
}

[numthreads(64, 1, 1)]
void Reset(uint3 id : SV_DispatchThreadID)
{
    if (id.x == 0)
    {
        scatter_dispatch_args[0] = 0;
        scatter_dispatch_args[1] = 1;
        scatter_dispatch_args[2] = 1;
    }
    if (id.x < scatter_batch_count)
    {
        scatter_draw_args[id.x].instance_count = 0;
    }
}

[numthreads(8, 8, 1)]
void CullTiles(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= (uint)scatter_tiles_side))
    {
        return;
    }

    int2 tile = ScatterFirstTile() + (int2)id.xy;
    float2 tile_min = tile * scatter_tile_size;
    float2 tile_max = tile_min + scatter_tile_size;
    float3 world_min = TerrainToWorldPosition(tile_min);
    float3 world_max = TerrainToWorldPosition(tile_max);
    if (distance(clamp(scatter_camera.xz, world_min.xz, world_max.xz), scatter_camera.xz) > scatter_range)
    {
        return;
    }

    // Height bounds from the corners and the middle; the details and the mesh add a margin
    float h00 = TerrainWorldHeight(world_min);
    float h11 = TerrainWorldHeight(world_max);
    float h10 = TerrainWorldHeight(float3(world_max.x, 0, world_min.z));
    float h01 = TerrainWorldHeight(float3(world_min.x, 0, world_max.z));
    float hc = TerrainWorldHeight((world_min + world_max) * 0.5);
    float low = min(min(min(h00, h11), min(h10, h01)), hc) - 1;
    float high = max(max(max(h00, h11), max(h10, h01)), hc) + 1;
    float3 box_min = float3(world_min.x, low, world_min.z);
    float3 box_max = float3(world_max.x, high + scatter_max_height, world_max.z);

    float nearest = distance(clamp(scatter_camera, box_min, box_max), scatter_camera);
    // Near details casting shadows are needed outside the view too
    bool shadow_casters = scatter_shadows > 0.5 && nearest < scatter_detailed_distance;
    if (nearest > scatter_range || !(shadow_casters || ScatterInFrustum(box_min, box_max)))
    {
        return;
    }

    // Behind a hill when the terrain hides the middle and every corner of the top of the tile.
    // Tiles around the camera are always drawn
    if (scatter_occlusion > 0.5 && nearest > scatter_tile_size * 2)
    {
        float top = box_max.y;
        bool visible = ScatterInSight(float3((world_min.x + world_max.x) * 0.5, top, (world_min.z + world_max.z) * 0.5))
            || ScatterInSight(float3(world_min.x, top, world_min.z)) || ScatterInSight(float3(world_max.x, top, world_min.z))
            || ScatterInSight(float3(world_min.x, top, world_max.z)) || ScatterInSight(float3(world_max.x, top, world_max.z));
        if (!visible)
        {
            return;
        }
    }

    uint index;
    InterlockedAdd(scatter_dispatch_args[0], 1, index);
    scatter_visible_tiles[index] = int4(tile, asint(nearest), 0);
}

[numthreads(64, 1, 1)]
void ClampArgs(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= scatter_batch_count)
    {
        return;
    }

    ScatterBatch part = scatter_batches[id.x];
    scatter_draw_args[id.x].index_count = min(scatter_draw_args[id.x].instance_count, part.capacity) * part.indices_per_instance;
    scatter_draw_args[id.x].instance_count = 1;
}

#endif
