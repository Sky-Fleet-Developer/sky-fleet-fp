using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameEditor.Trees
{
    public static partial class CyanTreeGreybox
    {
        /// <summary>
        /// The crystal of a tree as one solid: every crystal (coat, columns, shards, collars) is a hexagonal prism or a
        /// chain of them, the union of all of them is a signed distance field on a grid, and surface nets turn it into a
        /// single outer surface. Crystals that overlap fuse into one shape, so there are no faces inside of it (the
        /// translucent crystal would show them), and what is below the floor is cut away.
        /// Edges are rounded by about a cell. Each vertex takes the colors of the crystal it lies on, as
        /// CyanCrystal.hlsl reads them: r random per crystal, g along it, b age, a thickness.
        /// </summary>
        private class CrystalField
        {
            private const float Apothem = 0.8660254f; // of a hexagon, in circumradii

            private struct Segment
            {
                public Vector3 Start, Axis, U, V;
                public float Length, StartRadius, EndRadius, StartExtension, EndExtension;
                public float Id, AlongStart, AlongEnd;
                /// <summary>The end is broken: cut by two planes at an angle instead of a flat cap.</summary>
                public bool Broken;
                public Vector3 CutNormal1, CutNormal2;
                public float CutOffset2;
                public Vector3 Min, Max;
            }

            private readonly List<Segment> _segments = new ();

            /// <summary>
            /// A prism chain along points with a radius at each of them (a hexagon's corner radius). The first end is flat
            /// (it is buried), the last end is broken when asked.
            /// </summary>
            public void AddTube(Vector3[] points, float[] radii, float id, Random rng, bool brokenTip)
            {
                int count = points.Length;
                if (count < 2)
                {
                    return;
                }

                Vector3 firstAxis = (points[1] - points[0]).normalized;
                Vector3 u = Perpendicular(firstAxis, id * Mathf.PI * 2);
                for (int i = 0; i < count - 1; i++)
                {
                    Vector3 axis = (points[i + 1] - points[i]).normalized;
                    float length = Vector3.Distance(points[i], points[i + 1]);
                    if (length < 1e-4f)
                    {
                        continue;
                    }

                    u = (u - axis * Vector3.Dot(u, axis)).normalized;
                    float widest = Mathf.Max(radii[i], radii[i + 1]);
                    // Bends of a chain leave wedges between the prisms; reaching past the joints covers them
                    float overlap = widest * 0.7f;
                    var segment = new Segment
                    {
                        Start = points[i], Axis = axis, U = u, V = Vector3.Cross(axis, u), Length = length,
                        StartRadius = radii[i], EndRadius = radii[i + 1],
                        StartExtension = i > 0 ? overlap : 0, EndExtension = i < count - 2 ? overlap : 0,
                        Id = id, AlongStart = i / (float) (count - 1), AlongEnd = (i + 1) / (float) (count - 1),
                    };

                    if (brokenTip && i == count - 2)
                    {
                        segment.Broken = true;
                        Vector3 first = Perpendicular(axis, (float) rng.NextDouble() * Mathf.PI * 2);
                        Vector3 second = Perpendicular(axis, (float) rng.NextDouble() * Mathf.PI * 2);
                        float tilt1 = Mathf.Lerp(12, 35, (float) rng.NextDouble()) * Mathf.Deg2Rad;
                        float tilt2 = Mathf.Lerp(12, 35, (float) rng.NextDouble()) * Mathf.Deg2Rad;
                        segment.CutNormal1 = (axis * Mathf.Cos(tilt1) + first * Mathf.Sin(tilt1)).normalized;
                        segment.CutNormal2 = (axis * Mathf.Cos(tilt2) + second * Mathf.Sin(tilt2)).normalized;
                        segment.CutOffset2 = widest * Mathf.Lerp(0.3f, 0.7f, (float) rng.NextDouble());
                    }

                    float reach = widest + overlap + length * 0.05f;
                    Vector3 end = points[i + 1];
                    segment.Min = Vector3.Min(points[i], end) - Vector3.one * reach;
                    segment.Max = Vector3.Max(points[i], end) + Vector3.one * reach;
                    _segments.Add(segment);
                }
            }

            // Not an exact distance, but zero on the surface, negative inside, which is what the surface needs
            private static float Distance(in Segment s, Vector3 p)
            {
                Vector3 d = p - s.Start;
                float t = Vector3.Dot(d, s.Axis);
                float lx = Vector3.Dot(d, s.U);
                float ly = Vector3.Dot(d, s.V);
                float r = Mathf.Lerp(s.StartRadius, s.EndRadius, Mathf.Clamp01(t / s.Length));
                // Hexagon whose corners lie on the axes u and u turned by 60 degrees: its sides face 30, 90 and 150 degrees
                float hex = Mathf.Max(Mathf.Abs(lx * Apothem + ly * 0.5f),
                    Mathf.Max(Mathf.Abs(ly), Mathf.Abs(-lx * Apothem + ly * 0.5f))) - r * Apothem;
                float bottom = -t - s.StartExtension;
                float top;
                if (s.Broken)
                {
                    Vector3 fromEnd = d - s.Axis * s.Length;
                    top = Mathf.Max(Vector3.Dot(fromEnd, s.CutNormal1),
                        Vector3.Dot(fromEnd + s.Axis * s.CutOffset2, s.CutNormal2));
                }
                else
                {
                    top = t - s.Length - s.EndExtension;
                }

                return Mathf.Max(hex, Mathf.Max(bottom, top));
            }

            /// <summary>
            /// Meshes the union of the crystals: vertices, normals, uvs, colors and triangles (front faces outwards) are
            /// appended to the lists. Nothing is made below floorY.
            /// </summary>
            public void Build(float cell, float floorY, float age, List<Vector3> vertices, List<Vector3> normals,
                List<Vector2> uvs, List<Color> colors, List<int> triangles)
            {
                if (_segments.Count == 0)
                {
                    return;
                }

                Vector3 min = Vector3.one * float.MaxValue;
                Vector3 max = Vector3.one * float.MinValue;
                foreach (Segment segment in _segments)
                {
                    min = Vector3.Min(min, segment.Min);
                    max = Vector3.Max(max, segment.Max);
                }
                min.y = Mathf.Max(min.y, floorY);

                // Keep the grid at a size the editor handles
                Vector3 size = max - min;
                while ((size.x / cell + 3) * (size.y / cell + 3) * (size.z / cell + 3) > 6e6f)
                {
                    cell *= 1.25f;
                }

                min -= Vector3.one * cell;
                int px = Mathf.CeilToInt(size.x / cell) + 3;
                int py = Mathf.CeilToInt(size.y / cell) + 3;
                int pz = Mathf.CeilToInt(size.z / cell) + 3;
                var field = new float[px * py * pz];
                var owner = new int[field.Length];
                for (int i = 0; i < field.Length; i++)
                {
                    field[i] = 1000;
                    owner[i] = -1;
                }

                for (int k = 0; k < _segments.Count; k++)
                {
                    Segment s = _segments[k];
                    int x0 = Mathf.Max(0, Mathf.FloorToInt((s.Min.x - min.x) / cell));
                    int y0 = Mathf.Max(0, Mathf.FloorToInt((s.Min.y - min.y) / cell));
                    int z0 = Mathf.Max(0, Mathf.FloorToInt((s.Min.z - min.z) / cell));
                    int x1 = Mathf.Min(px - 1, Mathf.CeilToInt((s.Max.x - min.x) / cell));
                    int y1 = Mathf.Min(py - 1, Mathf.CeilToInt((s.Max.y - min.y) / cell));
                    int z1 = Mathf.Min(pz - 1, Mathf.CeilToInt((s.Max.z - min.z) / cell));
                    for (int z = z0; z <= z1; z++)
                    {
                        for (int y = y0; y <= y1; y++)
                        {
                            for (int x = x0; x <= x1; x++)
                            {
                                int index = (z * py + y) * px + x;
                                float d = Distance(s, min + new Vector3(x, y, z) * cell);
                                if (d < field[index])
                                {
                                    field[index] = d;
                                    owner[index] = k;
                                }
                            }
                        }
                    }
                }

                // Outside at the border of the grid and below the floor: the surface closes there
                for (int z = 0; z < pz; z++)
                {
                    for (int y = 0; y < py; y++)
                    {
                        for (int x = 0; x < px; x++)
                        {
                            bool border = x == 0 || y == 0 || z == 0 || x == px - 1 || y == py - 1 || z == pz - 1;
                            int index = (z * py + y) * px + x;
                            if ((border || min.y + y * cell < floorY) && field[index] < 0.001f)
                            {
                                field[index] = 0.001f;
                            }
                        }
                    }
                }

                int nx = px - 1, ny = py - 1, nz = pz - 1;
                var cellVertex = new int[nx * ny * nz];
                for (int i = 0; i < cellVertex.Length; i++)
                {
                    cellVertex[i] = -1;
                }

                int firstVertex = vertices.Count;
                var corners = new float[8];
                for (int z = 0; z < nz; z++)
                {
                    for (int y = 0; y < ny; y++)
                    {
                        for (int x = 0; x < nx; x++)
                        {
                            int inside = 0;
                            int deepest = 0;
                            for (int c = 0; c < 8; c++)
                            {
                                int index = ((z + ((c >> 2) & 1)) * py + y + ((c >> 1) & 1)) * px + x + (c & 1);
                                corners[c] = field[index];
                                inside += corners[c] < 0 ? 1 : 0;
                                if (corners[c] < corners[deepest])
                                {
                                    deepest = c;
                                }
                            }
                            if (inside == 0 || inside == 8)
                            {
                                continue;
                            }

                            // Average of where the surface crosses the edges of the cell
                            Vector3 sum = Vector3.zero;
                            int crossings = 0;
                            for (int e = 0; e < 12; e++)
                            {
                                int a, b;
                                EdgeCorners(e, out a, out b);
                                if ((corners[a] < 0) == (corners[b] < 0))
                                {
                                    continue;
                                }

                                float t = corners[a] / (corners[a] - corners[b]);
                                sum += Vector3.Lerp(CornerOffset(a), CornerOffset(b), t);
                                crossings++;
                            }

                            Vector3 position = min + (new Vector3(x, y, z) + sum / crossings) * cell;
                            int deepestIndex = ((z + ((deepest >> 2) & 1)) * py + y + ((deepest >> 1) & 1)) * px + x + (deepest & 1);
                            cellVertex[(z * ny + y) * nx + x] = vertices.Count;
                            vertices.Add(position);
                            normals.Add(Gradient(field, px, py, pz, min, cell, position));
                            uvs.Add(new Vector2(position.x + position.z, position.y));
                            colors.Add(ColorAt(owner[deepestIndex], position, age));
                        }
                    }
                }

                // A quad for every edge of the grid the surface crosses, from the four cells around it
                for (int z = 0; z < pz; z++)
                {
                    for (int y = 0; y < py; y++)
                    {
                        for (int x = 0; x < px; x++)
                        {
                            float here = field[(z * py + y) * px + x];
                            if (x < px - 1 && y > 0 && z > 0 && y < py - 1 && z < pz - 1)
                            {
                                float there = field[(z * py + y) * px + x + 1];
                                if ((here < 0) != (there < 0))
                                {
                                    AddQuad(cellVertex, nx, ny, vertices, normals, triangles, here < 0,
                                        new Vector3Int(x, y - 1, z - 1), new Vector3Int(x, y, z - 1), new Vector3Int(x, y, z), new Vector3Int(x, y - 1, z));
                                }
                            }
                            if (y < py - 1 && x > 0 && z > 0 && x < px - 1 && z < pz - 1)
                            {
                                float there = field[(z * py + y + 1) * px + x];
                                if ((here < 0) != (there < 0))
                                {
                                    AddQuad(cellVertex, nx, ny, vertices, normals, triangles, here < 0,
                                        new Vector3Int(x - 1, y, z - 1), new Vector3Int(x - 1, y, z), new Vector3Int(x, y, z), new Vector3Int(x, y, z - 1));
                                }
                            }
                            if (z < pz - 1 && x > 0 && y > 0 && x < px - 1 && y < py - 1)
                            {
                                float there = field[((z + 1) * py + y) * px + x];
                                if ((here < 0) != (there < 0))
                                {
                                    AddQuad(cellVertex, nx, ny, vertices, normals, triangles, here < 0,
                                        new Vector3Int(x - 1, y - 1, z), new Vector3Int(x, y - 1, z), new Vector3Int(x, y, z), new Vector3Int(x - 1, y, z));
                                }
                            }
                        }
                    }
                }
            }

            // Four cells around an edge in the order that faces away from the inside, when the inside is at the start
            private static void AddQuad(int[] cellVertex, int nx, int ny, List<Vector3> vertices, List<Vector3> normals,
                List<int> triangles, bool startInside, Vector3Int c0, Vector3Int c1, Vector3Int c2, Vector3Int c3)
            {
                int v0 = cellVertex[(c0.z * ny + c0.y) * nx + c0.x];
                int v1 = cellVertex[(c1.z * ny + c1.y) * nx + c1.x];
                int v2 = cellVertex[(c2.z * ny + c2.y) * nx + c2.x];
                int v3 = cellVertex[(c3.z * ny + c3.y) * nx + c3.x];
                if (v0 < 0 || v1 < 0 || v2 < 0 || v3 < 0)
                {
                    return;
                }

                if (!startInside)
                {
                    int swap = v1;
                    v1 = v3;
                    v3 = swap;
                }

                AddTriangle(vertices, normals, triangles, v0, v1, v2);
                AddTriangle(vertices, normals, triangles, v0, v2, v3);
            }

            // The order of the quad already faces away from the inside; deciding it per triangle would break the surface
            private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, int a, int b, int c)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            private Color ColorAt(int segmentIndex, Vector3 position, float age)
            {
                if (segmentIndex < 0)
                {
                    return new Color(0.5f, 0.5f, age, 0.5f);
                }

                Segment s = _segments[segmentIndex];
                float f = Mathf.Clamp01(Vector3.Dot(position - s.Start, s.Axis) / s.Length);
                float along = Mathf.Lerp(s.AlongStart, s.AlongEnd, f);
                float radius = Mathf.Lerp(s.StartRadius, s.EndRadius, f);
                return new Color(s.Id, along, age, Mathf.Clamp01(radius / 0.9f));
            }

            // Direction in which the field grows, from the grid
            private static Vector3 Gradient(float[] field, int px, int py, int pz, Vector3 min, float cell, Vector3 p)
            {
                float h = cell * 0.75f;
                Vector3 g = new Vector3(
                    Sample(field, px, py, pz, min, cell, p + Vector3.right * h) - Sample(field, px, py, pz, min, cell, p - Vector3.right * h),
                    Sample(field, px, py, pz, min, cell, p + Vector3.up * h) - Sample(field, px, py, pz, min, cell, p - Vector3.up * h),
                    Sample(field, px, py, pz, min, cell, p + Vector3.forward * h) - Sample(field, px, py, pz, min, cell, p - Vector3.forward * h));
                return g.sqrMagnitude > 1e-12f ? g.normalized : Vector3.up;
            }

            private static float Sample(float[] field, int px, int py, int pz, Vector3 min, float cell, Vector3 p)
            {
                Vector3 g = (p - min) / cell;
                g.x = Mathf.Clamp(g.x, 0, px - 1.001f);
                g.y = Mathf.Clamp(g.y, 0, py - 1.001f);
                g.z = Mathf.Clamp(g.z, 0, pz - 1.001f);
                int x = (int) g.x, y = (int) g.y, z = (int) g.z;
                float fx = g.x - x, fy = g.y - y, fz = g.z - z;
                float At(int ix, int iy, int iz) => field[(iz * py + iy) * px + ix];
                float x00 = Mathf.Lerp(At(x, y, z), At(x + 1, y, z), fx);
                float x10 = Mathf.Lerp(At(x, y + 1, z), At(x + 1, y + 1, z), fx);
                float x01 = Mathf.Lerp(At(x, y, z + 1), At(x + 1, y, z + 1), fx);
                float x11 = Mathf.Lerp(At(x, y + 1, z + 1), At(x + 1, y + 1, z + 1), fx);
                return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
            }

            // Corners of a cell: bit 0 is x, bit 1 is y, bit 2 is z
            private static Vector3 CornerOffset(int c) => new Vector3(c & 1, (c >> 1) & 1, (c >> 2) & 1);

            private static void EdgeCorners(int edge, out int a, out int b)
            {
                switch (edge)
                {
                    case 0: a = 0; b = 1; break;
                    case 1: a = 2; b = 3; break;
                    case 2: a = 4; b = 5; break;
                    case 3: a = 6; b = 7; break;
                    case 4: a = 0; b = 2; break;
                    case 5: a = 1; b = 3; break;
                    case 6: a = 4; b = 6; break;
                    case 7: a = 5; b = 7; break;
                    case 8: a = 0; b = 4; break;
                    case 9: a = 1; b = 5; break;
                    case 10: a = 2; b = 6; break;
                    default: a = 3; b = 7; break;
                }
            }
        }
    }
}
