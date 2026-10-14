using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>One site of the power diagram. Layout matches BiomeSite in BiomeDiagram.hlsl.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BiomeSite
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        /// <summary>Terrain space x, z in meters.</summary>
        public Vector2 Position;
        public float Radius;
        public float BlendDistance;
        /// <summary>Index of the biome in <see cref="IBiomeSet.Biomes"/>.</summary>
        public int Biome;
        /// <summary>Cosine and sine of the site rotation around Y in terrain space.</summary>
        public Vector2 Rotation;
        /// <summary><see cref="BiomeBlendMode"/> of the biome.</summary>
        public int BlendMode;

        public bool IsLayer => BlendMode != (int)BiomeBlendMode.Normal;

        /// <summary>Converts a terrain space position into the unrotated space of the site, relative to the site.</summary>
        public Vector2 ToLocal(Vector2 position)
        {
            Vector2 delta = position - Position;
            return new Vector2(delta.x * Rotation.x - delta.y * Rotation.y, delta.x * Rotation.y + delta.y * Rotation.x);
        }
    }

    /// <summary>
    /// Power diagram of the biome sites and the blend weights derived from it.
    /// Mirrors BiomeDiagram.hlsl, so the CPU (gizmos, chunk culling) and the GPU agree.
    /// <para>
    /// Weight of a site a: product over the sites c of other biomes of smoothstep(t),
    /// t = (sd + blend_c) / (blend_a + blend_c), where sd is the signed distance from the a|c power bisector
    /// (positive on the a side). At the border both sides weigh 50%; at blend_a inside the cell of a the weight is 100%.
    /// Weight of a biome: the max weight of its sites. Normal biomes are normalized among themselves;
    /// layers (<see cref="BiomeBlendMode"/>) keep their own weight.
    /// </para>
    /// <para>
    /// Normal sites only compete with normal sites, so normal biomes cover the world without the layers.
    /// Layer sites compete with every site, so a layer covers its cell of the full diagram.
    /// </para>
    /// </summary>
    public static class BiomeDiagram
    {
        private static readonly List<BiomeSitePose> SitePosesCache = new ();

        /// <summary>Collects the sites of all biomes in blending order.</summary>
        public static void CollectSites(IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, List<BiomeSite> result)
        {
            result.Clear();
            for (int i = 0; i < biomes.Count; i++)
            {
                IBiome biome = biomes[i];
                SitePosesCache.Clear();
                biome.GetSites(SitePosesCache);
                foreach (BiomeSitePose pose in SitePosesCache)
                {
                    Vector3 local = worldToTerrain.MultiplyPoint3x4(pose.Position);
                    Vector3 forward = worldToTerrain.MultiplyVector(Quaternion.Euler(0, pose.Yaw, 0) * Vector3.forward);
                    // Unity yaw turns +z towards +x
                    float yaw = Mathf.Atan2(forward.x, forward.z);
                    result.Add(new BiomeSite
                    {
                        Position = new Vector2(local.x, local.z),
                        Radius = Mathf.Max(0, biome.Radius),
                        BlendDistance = Mathf.Max(0, biome.BlendDistance),
                        Biome = i,
                        Rotation = new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)),
                        BlendMode = (int)biome.BlendMode,
                    });
                }
            }
        }

        /// <summary>Signed distance from the power bisector of a and c, positive on the side of a.</summary>
        public static float SignedDistance(in BiomeSite a, in BiomeSite c, Vector2 position)
        {
            Vector2 delta = a.Position - c.Position;
            float length = delta.magnitude;
            if (length < 1e-3f)
            {
                // Coincident sites: the larger radius takes the whole plane
                return a.Radius >= c.Radius ? float.MaxValue : float.MinValue;
            }

            Vector2 middle = (a.Position + c.Position) * 0.5f;
            return Vector2.Dot(position - middle, delta / length) + (a.Radius * a.Radius - c.Radius * c.Radius) / (2f * length);
        }

        /// <summary>True when c lowers the weight of a.</summary>
        public static bool Competes(in BiomeSite a, in BiomeSite c)
        {
            return c.Biome != a.Biome && (a.IsLayer || !c.IsLayer);
        }

        /// <summary>Weight factor of a against c at a signed distance sd from their bisector.</summary>
        public static float Transition(in BiomeSite a, in BiomeSite c, float sd)
        {
            float band = a.BlendDistance + c.BlendDistance;
            if (band <= 0)
            {
                return sd >= 0 ? 1 : 0;
            }

            float t = Mathf.Clamp01((sd + c.BlendDistance) / band);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Biome weights at a terrain space position, normal biomes normalized among themselves. weights.Count = biome count.
        /// </summary>
        public static void EvaluateWeights(IReadOnlyList<BiomeSite> sites, int biomeCount, Vector2 position, List<float> weights)
        {
            weights.Clear();
            for (int i = 0; i < biomeCount; i++)
            {
                weights.Add(0);
            }

            float total = 0;
            for (int a = 0; a < sites.Count; a++)
            {
                float weight = 1;
                for (int c = 0; c < sites.Count && weight > 0; c++)
                {
                    if (Competes(sites[a], sites[c]))
                    {
                        weight *= Transition(sites[a], sites[c], SignedDistance(sites[a], sites[c], position));
                    }
                }

                int biome = sites[a].Biome;
                weights[biome] = Mathf.Max(weights[biome], weight);
            }

            var isLayer = new bool[biomeCount];
            foreach (BiomeSite site in sites)
            {
                isLayer[site.Biome] = site.IsLayer;
            }

            for (int i = 0; i < biomeCount; i++)
            {
                if (!isLayer[i])
                {
                    total += weights[i];
                }
            }

            if (total <= 0)
            {
                return;
            }

            for (int i = 0; i < biomeCount; i++)
            {
                if (!isLayer[i])
                {
                    weights[i] /= total;
                }
            }
        }

        /// <summary>Entries with this flag only lower the weights of the other entries, they have no weight of their own.</summary>
        public const uint CompetitorFlag = 0x80000000u;

        /// <summary>
        /// Selects the sites that matter inside a terrain space rectangle, in blending order.
        /// A site is weighted when its weight can be above zero inside the rectangle.
        /// A site is a competitor (<see cref="CompetitorFlag"/>) when it lowers the weight of a weighted site there.
        /// Signed distances are linear in position, so checking the rectangle corners is exact.
        /// </summary>
        public static void SelectSites(IReadOnlyList<BiomeSite> sites, Rect rect, List<uint> result, List<bool> weightedCache)
        {
            result.Clear();
            weightedCache.Clear();
            for (int a = 0; a < sites.Count; a++)
            {
                bool weighted = true;
                for (int c = 0; c < sites.Count && weighted; c++)
                {
                    if (!Competes(sites[a], sites[c]))
                    {
                        continue;
                    }

                    GetRange(sites[a], sites[c], rect, out _, out float max);
                    weighted = Transition(sites[a], sites[c], max) > 0;
                }
                weightedCache.Add(weighted);
            }

            for (int c = 0; c < sites.Count; c++)
            {
                if (weightedCache[c])
                {
                    result.Add((uint)c);
                    continue;
                }

                for (int a = 0; a < sites.Count; a++)
                {
                    if (!weightedCache[a] || !Competes(sites[a], sites[c]))
                    {
                        continue;
                    }

                    GetRange(sites[a], sites[c], rect, out float min, out _);
                    if (Transition(sites[a], sites[c], min) < 1)
                    {
                        result.Add((uint)c | CompetitorFlag);
                        break;
                    }
                }
            }
        }

        private static void GetRange(in BiomeSite a, in BiomeSite c, Rect rect, out float min, out float max)
        {
            float d0 = SignedDistance(a, c, new Vector2(rect.xMin, rect.yMin));
            float d1 = SignedDistance(a, c, new Vector2(rect.xMax, rect.yMin));
            float d2 = SignedDistance(a, c, new Vector2(rect.xMin, rect.yMax));
            float d3 = SignedDistance(a, c, new Vector2(rect.xMax, rect.yMax));
            min = Mathf.Min(Mathf.Min(d0, d1), Mathf.Min(d2, d3));
            max = Mathf.Max(Mathf.Max(d0, d1), Mathf.Max(d2, d3));
        }

        /// <summary>
        /// Polygon where sd(site, c) >= offset for every site c of another biome, clipped by bounds.
        /// offset 0: the part of the biome cell around the site; offset = blend distance: where the site weighs 100%.
        /// </summary>
        public static void GetCellPolygon(IReadOnlyList<BiomeSite> sites, int site, float offset, Rect bounds, List<Vector2> polygon)
        {
            polygon.Clear();
            polygon.Add(new Vector2(bounds.xMin, bounds.yMin));
            polygon.Add(new Vector2(bounds.xMax, bounds.yMin));
            polygon.Add(new Vector2(bounds.xMax, bounds.yMax));
            polygon.Add(new Vector2(bounds.xMin, bounds.yMax));

            for (int c = 0; c < sites.Count && polygon.Count > 0; c++)
            {
                if (Competes(sites[site], sites[c]))
                {
                    Clip(polygon, sites[site], sites[c], offset);
                }
            }
        }

        private static readonly List<Vector2> ClipCache = new ();

        // Sutherland–Hodgman against the half plane sd(a, c) >= offset
        private static void Clip(List<Vector2> polygon, in BiomeSite a, in BiomeSite c, float offset)
        {
            ClipCache.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                float dc = SignedDistance(a, c, current) - offset;
                float dn = SignedDistance(a, c, next) - offset;
                if (dc >= 0)
                {
                    ClipCache.Add(current);
                }

                if (dc >= 0 != dn >= 0)
                {
                    ClipCache.Add(Vector2.Lerp(current, next, dc / (dc - dn)));
                }
            }

            polygon.Clear();
            polygon.AddRange(ClipCache);
        }
    }
}
