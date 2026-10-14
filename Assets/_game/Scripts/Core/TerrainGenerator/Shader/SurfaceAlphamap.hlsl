#ifndef SURFACE_ALPHAMAP
#define SURFACE_ALPHAMAP

// Encoded terrain alphamap (RGBA8): the two strongest surface types of a pixel.
//   r = id of the stronger surface / 255
//   g = id of the weaker surface / 255
//   b = share of the weaker surface, w1 / (w0 + w1) in [0, 0.5], stored * 2
//   a = 1 when the pixel has a surface, 0 when it has none
// Ids index Core.TerrainGenerator.Surfaces.TerrainSurfaces. Encoded pixels can't be interpolated:
// read them with Load and blend the decoded surfaces instead.

#define SURFACE_MAX_CANDIDATES 8

// Inlined calls right after SurfacesBegin have loops the compiler sees as empty: nothing to warn about
#pragma warning(disable : 3557)

// Surfaces collected for one pixel. Plain static arrays: FXC can't write arrays inside a struct at a dynamic index
static int surface_candidate_count;
static int surface_candidate_id[SURFACE_MAX_CANDIDATES];
static float surface_candidate_weight[SURFACE_MAX_CANDIDATES];

void SurfacesBegin()
{
    surface_candidate_count = 0;
}

// Adds weight to a surface. When the list is full, the weakest candidate gives way to a stronger one
void SurfacesAdd(int id, float weight)
{
    if (id < 0 || weight <= 0)
    {
        return;
    }

    int weakest = 0;
    for (int i = 0; i < surface_candidate_count; i++)
    {
        if (surface_candidate_id[i] == id)
        {
            surface_candidate_weight[i] += weight;
            return;
        }
        if (surface_candidate_weight[i] < surface_candidate_weight[weakest])
        {
            weakest = i;
        }
    }

    if (surface_candidate_count < SURFACE_MAX_CANDIDATES)
    {
        surface_candidate_id[surface_candidate_count] = id;
        surface_candidate_weight[surface_candidate_count] = weight;
        surface_candidate_count++;
    }
    else if (weight > surface_candidate_weight[weakest])
    {
        surface_candidate_id[weakest] = id;
        surface_candidate_weight[weakest] = weight;
    }
}

// Channel weights of a source alphamap; channel_ids < 0 are ignored channels
void SurfacesAddChannels(int4 channel_ids, float4 weights, float scale)
{
    SurfacesAdd(channel_ids.x, weights.x * scale);
    SurfacesAdd(channel_ids.y, weights.y * scale);
    SurfacesAdd(channel_ids.z, weights.z * scale);
    SurfacesAdd(channel_ids.w, weights.w * scale);
}

void SurfaceDecode(float4 encoded, out int id0, out int id1, out float weight0, out float weight1)
{
    id0 = (int)round(encoded.r * 255);
    id1 = (int)round(encoded.g * 255);
    weight1 = encoded.a > 0.5 ? encoded.b * 0.5 : 0;
    weight0 = encoded.a > 0.5 ? 1 - weight1 : 0;
}

void SurfacesAddEncoded(float4 encoded, float scale)
{
    int id0, id1;
    float weight0, weight1;
    SurfaceDecode(encoded, id0, id1, weight0, weight1);
    SurfacesAdd(id0, weight0 * scale);
    SurfacesAdd(id1, weight1 * scale);
}

// Moves the count strongest candidates to the front, strongest first
void SurfacesSortStrongest(int count)
{
    for (int i = 0; i < min(count, surface_candidate_count); i++)
    {
        int strongest = i;
        for (int j = i + 1; j < surface_candidate_count; j++)
        {
            if (surface_candidate_weight[j] > surface_candidate_weight[strongest])
            {
                strongest = j;
            }
        }

        int id = surface_candidate_id[i];
        float weight = surface_candidate_weight[i];
        surface_candidate_id[i] = surface_candidate_id[strongest];
        surface_candidate_weight[i] = surface_candidate_weight[strongest];
        surface_candidate_id[strongest] = id;
        surface_candidate_weight[strongest] = weight;
    }
}

// Encodes the two strongest candidates
float4 SurfacesEncode()
{
    SurfacesSortStrongest(2);
    if (surface_candidate_count == 0)
    {
        return 0;
    }

    int id0 = surface_candidate_id[0];
    float weight0 = surface_candidate_weight[0];
    int id1 = surface_candidate_count > 1 ? surface_candidate_id[1] : id0;
    float weight1 = surface_candidate_count > 1 ? surface_candidate_weight[1] : 0;
    float share = weight1 / max(weight0 + weight1, 1e-6);
    return float4(id0 / 255.0, id1 / 255.0, saturate(share * 2), 1);
}

#endif
