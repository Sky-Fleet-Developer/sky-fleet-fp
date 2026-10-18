using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>One site of the power diagram. Layout matches BiomeSite in BiomeDiagram.hlsl.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BiomeSite
    {
        public const int Stride = sizeof(float) * 11 + sizeof(int) * 7;

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
        /// <summary>Added to the heights of the biome data placed by this site, in meters. Terrain space y of the site.</summary>
        public float HeightOffset;
        /// <summary><see cref="BiomeShapeType"/> of the biome. Anything but Cell takes no part in the diagram.</summary>
        public int Shape;
        /// <summary>Rectangle: half extents. Circle: x is the radius.</summary>
        public Vector2 ShapeSize;
        /// <summary>Polygon: corners from <see cref="BiomeShapeData.Points"/>.</summary>
        public int PointStart;
        public int PointCount;
        /// <summary>First of <see cref="BiomeShape.CurveSamples"/> fade curve samples in <see cref="BiomeShapeData.Curves"/>.</summary>
        public int CurveOffset;
        public float FadeDistance;
        /// <summary>Distance from the site to the farthest point of the shape.</summary>
        public float BoundsRadius;
        /// <summary>1 when the border is bent by the edge noise.</summary>
        public int BendShape;

        public bool IsLayer => BlendMode != (int)BiomeBlendMode.Normal;
        public bool IsShaped => Shape != (int)BiomeShapeType.Cell;

        /// <summary>Converts a position in the unrotated space of the site into a terrain space position.</summary>
        public Vector2 ToWorld(Vector2 local)
        {
            return Position + new Vector2(local.x * Rotation.x + local.y * Rotation.y, -local.x * Rotation.y + local.y * Rotation.x);
        }

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
    /// <para>
    /// Shaped sites (<see cref="BiomeShape"/>) are outside the diagram: they compete with nobody, and their weight comes
    /// from the distance to the border of the shape and the fade curve.
    /// </para>
    /// </summary>
    public static class BiomeDiagram
    {
        private static readonly List<BiomeSitePose> SitePosesCache = new ();

        /// <summary>
        /// Collects the sites of all biomes in blending order. The polygons and fade curves of shaped sites go to
        /// <paramref name="shapes"/>, which is cleared first.
        /// </summary>
        public static void CollectSites(IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, List<BiomeSite> result,
            BiomeShapeData shapes)
        {
            result.Clear();
            shapes.Clear();
            for (int i = 0; i < biomes.Count; i++)
            {
                IBiome biome = biomes[i];
                BiomeShape shape = biome.Shape;
                bool shaped = shape is { IsShaped: true };
                if (shaped && !shape.IsValid)
                {
                    continue;
                }

                SitePosesCache.Clear();
                biome.GetSites(SitePosesCache);
                foreach (BiomeSitePose pose in SitePosesCache)
                {
                    Vector3 local = worldToTerrain.MultiplyPoint3x4(pose.Position);
                    Vector3 forward = worldToTerrain.MultiplyVector(Quaternion.Euler(0, pose.Yaw, 0) * Vector3.forward);
                    // Unity yaw turns +z towards +x
                    float yaw = Mathf.Atan2(forward.x, forward.z);
                    var site = new BiomeSite
                    {
                        Position = new Vector2(local.x, local.z),
                        Radius = Mathf.Max(0, biome.Radius),
                        BlendDistance = Mathf.Max(0, biome.BlendDistance),
                        Biome = i,
                        Rotation = new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)),
                        BlendMode = (int)biome.BlendMode,
                        HeightOffset = local.y,
                    };

                    if (shaped)
                    {
                        AddShape(ref site, shape, shapes);
                    }
                    result.Add(site);
                }
            }
        }

        // A shaped biome is always a layer: it has no cell to share the world with
        private static void AddShape(ref BiomeSite site, BiomeShape shape, BiomeShapeData shapes)
        {
            if (site.BlendMode == (int)BiomeBlendMode.Normal)
            {
                site.BlendMode = (int)BiomeBlendMode.Override;
            }

            site.Shape = (int)shape.Type;
            site.FadeDistance = shape.FadeDistance;
            site.BoundsRadius = shape.GetBoundsRadius();
            site.BendShape = shape.BendByEdgeNoise ? 1 : 0;
            site.CurveOffset = shapes.Curves.Count;
            shape.AppendCurveSamples(shapes.Curves);
            switch (shape.Type)
            {
                case BiomeShapeType.Rectangle:
                    site.ShapeSize = new Vector2(Mathf.Abs(shape.Size.x), Mathf.Abs(shape.Size.y)) * 0.5f;
                    break;
                case BiomeShapeType.Circle:
                    site.ShapeSize = new Vector2(Mathf.Max(0, shape.Radius), 0);
                    break;
                case BiomeShapeType.Polygon:
                    site.PointStart = shapes.Points.Count;
                    site.PointCount = shape.Points.Count;
                    for (int i = 0; i < shape.Points.Count; i++)
                    {
                        shapes.Points.Add(shape.Points[i]);
                    }
                    break;
            }
        }

        /// <summary>
        /// Distance from a terrain space position to the border of a shaped site, in meters: positive inside.
        /// Mirrors BiomeShapeDistance in BiomeDiagram.hlsl.
        /// </summary>
        public static float ShapeDistance(in BiomeSite site, BiomeShapeData shapes, Vector2 position)
        {
            Vector2 q = site.ToLocal(position);
            switch ((BiomeShapeType)site.Shape)
            {
                case BiomeShapeType.Rectangle:
                    Vector2 d = new Vector2(Mathf.Abs(q.x), Mathf.Abs(q.y)) - site.ShapeSize;
                    return -(Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0));
                case BiomeShapeType.Circle:
                    return site.ShapeSize.x - q.magnitude;
                case BiomeShapeType.Polygon:
                    // Distance to the closest edge, negative inside (even-odd crossing count)
                    Vector2 first = shapes.Points[site.PointStart];
                    float best = (q - first).sqrMagnitude;
                    float sign = 1;
                    for (int i = 0, j = site.PointCount - 1; i < site.PointCount; j = i, i++)
                    {
                        Vector2 a = shapes.Points[site.PointStart + i];
                        Vector2 b = shapes.Points[site.PointStart + j];
                        Vector2 e = b - a;
                        Vector2 w = q - a;
                        Vector2 nearest = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(Vector2.Dot(e, e), 1e-8f));
                        best = Mathf.Min(best, nearest.sqrMagnitude);
                        bool c0 = q.y >= a.y;
                        bool c1 = q.y < b.y;
                        bool c2 = e.x * w.y > e.y * w.x;
                        if (c0 == c1 && c1 == c2)
                        {
                            sign = -sign;
                        }
                    }
                    return -sign * Mathf.Sqrt(best);
                default:
                    return float.MinValue;
            }
        }

        /// <summary>Weight of a shaped site at a terrain space position, without the edge noise. Mirrors BiomeShapeWeight.</summary>
        public static float ShapeWeight(in BiomeSite site, BiomeShapeData shapes, Vector2 position)
        {
            float distance = ShapeDistance(site, shapes, position);
            if (distance < 0)
            {
                return 0;
            }

            if (site.FadeDistance <= 0)
            {
                return 1;
            }

            float t = Mathf.Clamp01(distance / site.FadeDistance) * (BiomeShape.CurveSamples - 1);
            int i = Mathf.Min((int)t, BiomeShape.CurveSamples - 1);
            int next = Mathf.Min(i + 1, BiomeShape.CurveSamples - 1);
            return Mathf.Lerp(shapes.Curves[site.CurveOffset + i], shapes.Curves[site.CurveOffset + next], t - i);
        }

        /// <summary>
        /// Outline of a shaped site in terrain space, moved inward by <paramref name="inset"/> (meters; polygons ignore it).
        /// </summary>
        public static void GetShapeOutline(in BiomeSite site, BiomeShapeData shapes, float inset, List<Vector2> polygon)
        {
            polygon.Clear();
            switch ((BiomeShapeType)site.Shape)
            {
                case BiomeShapeType.Rectangle:
                    Vector2 h = Vector2.Max(site.ShapeSize - Vector2.one * inset, Vector2.zero);
                    polygon.Add(site.ToWorld(new Vector2(-h.x, -h.y)));
                    polygon.Add(site.ToWorld(new Vector2(h.x, -h.y)));
                    polygon.Add(site.ToWorld(new Vector2(h.x, h.y)));
                    polygon.Add(site.ToWorld(new Vector2(-h.x, h.y)));
                    break;
                case BiomeShapeType.Circle:
                    float radius = Mathf.Max(site.ShapeSize.x - inset, 0);
                    const int segments = 64;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / segments;
                        polygon.Add(site.ToWorld(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius));
                    }
                    break;
                case BiomeShapeType.Polygon:
                    for (int i = 0; i < site.PointCount; i++)
                    {
                        polygon.Add(site.ToWorld(shapes.Points[site.PointStart + i]));
                    }
                    break;
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
            return c.Biome != a.Biome && !a.IsShaped && !c.IsShaped && (a.IsLayer || !c.IsLayer);
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
        public static void EvaluateWeights(IReadOnlyList<BiomeSite> sites, BiomeShapeData shapes, int biomeCount, Vector2 position,
            List<float> weights)
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
                if (sites[a].IsShaped)
                {
                    weight = ShapeWeight(sites[a], shapes, position);
                }

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
        /// <param name="margin">
        /// Widens the rectangle on every side: weights are evaluated at positions shifted by up to this distance
        /// (<see cref="Settings.BiomeEdgeNoise.Margin"/>).
        /// </param>
        public static void SelectSites(IReadOnlyList<BiomeSite> sites, Rect rect, List<uint> result, List<bool> weightedCache,
            float margin = 0)
        {
            if (margin > 0)
            {
                rect = new Rect(rect.xMin - margin, rect.yMin - margin, rect.width + 2 * margin, rect.height + 2 * margin);
            }

            result.Clear();
            weightedCache.Clear();
            for (int a = 0; a < sites.Count; a++)
            {
                if (sites[a].IsShaped)
                {
                    // Conservative: the circle around the site that holds the shape, tested against the rectangle
                    Vector2 nearest = new Vector2(Mathf.Clamp(sites[a].Position.x, rect.xMin, rect.xMax),
                        Mathf.Clamp(sites[a].Position.y, rect.yMin, rect.yMax));
                    weightedCache.Add((nearest - sites[a].Position).magnitude <= sites[a].BoundsRadius);
                    continue;
                }

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
