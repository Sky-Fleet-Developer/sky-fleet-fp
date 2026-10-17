using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameEditor.Trees
{
    /// <summary>
    /// Builds greybox prefabs of the Cyan Tree for iterating its shape. The tree grows in short periods: a shoot grows
    /// and ends in a bud that opens into a succulent rosette; small lateral shoots with smaller buds grow at its base.
    /// Older shoots fork and are soon coated in crystal, which ends in broken, chaotic tips. Three stages (young,
    /// mature, ancient) with a few variants each. Everything is code and numbers here, so the shape is iterated by
    /// editing <see cref="StageOf"/> and running the menu again; assets keep their guids.
    /// </summary>
    public static partial class CyanTreeGreybox
    {
        private const string Folder = "Assets/_game/Prefabs/Trees/CyanGreybox";
        private const int Variants = 3;

        /// <summary>
        /// The crystal as one solid meshed from a distance field (CrystalField): no shells inside each other, but tens of
        /// thousands of triangles. Off while the crystal is opaque; plain prisms are used then.
        /// </summary>
        private static readonly bool SolidCrystal = false;
        private const int TrunkSub = 0;
        private const int CrystalSub = 1;
        private const int PetalSub = 2;

        private enum Stage { Young, Mature, Ancient }

        private struct StageParams
        {
            /// <summary>Length of the first shoot, meters.</summary>
            public Vector2 shootLength;
            public float lean;
            public float trunkBase;
            /// <summary>Segments of a shoot: the stalk swells at each of them.</summary>
            public int nodes;
            /// <summary>Crystal coat of a shoot: gap above the bark, thickness at the foot of the first shoot.</summary>
            public float sheathGap, sheathBase;
            /// <summary>Share of the youngest shoots the coat reaches; older shoots are coated all the way.</summary>
            public float terminalCover;
            /// <summary>Generations of forks after the first shoot, children per fork, their length relative to the parent.</summary>
            public int maxGeneration, forkCount;
            public float childLength;
            public Vector2 forkAngle;
            /// <summary>Lateral shoots at the base of a shoot: count, and length relative to the shoot.</summary>
            public int lateralMin, lateralMax;
            public Vector2 lateralLength;
            public int petalsPerRosette;
            public float petalLength, petalWidth;
            public int shards;
            public Vector2 shardLength, shardRadius;
            /// <summary>Crystal columns around the foot: height as a share of the tree, radius, splay from vertical (degrees).</summary>
            public int columns;
            public Vector2 columnHeight, columnRadius, columnSplay;
        }

        private static StageParams StageOf(Stage stage)
        {
            switch (stage)
            {
                case Stage.Young:
                    return new StageParams
                    {
                        shootLength = new Vector2(4.5f, 5.5f), lean = 0.1f, trunkBase = 0.12f, nodes = 4,
                        sheathGap = 0.02f, sheathBase = 0.05f, terminalCover = 0.35f,
                        maxGeneration = 0, forkCount = 0, childLength = 0.8f, forkAngle = new Vector2(15, 35),
                        lateralMin = 2, lateralMax = 3, lateralLength = new Vector2(0.1f, 0.4f),
                        petalsPerRosette = 8, petalLength = 0.55f, petalWidth = 0.16f,
                        shards = 2, shardLength = new Vector2(0.15f, 0.3f), shardRadius = new Vector2(0.03f, 0.05f),
                        columns = 3, columnHeight = new Vector2(0.06f, 0.14f), columnRadius = new Vector2(0.04f, 0.07f),
                        columnSplay = new Vector2(5, 15),
                    };
                case Stage.Mature:
                    return new StageParams
                    {
                        shootLength = new Vector2(3.6f, 4.2f), lean = 0.08f, trunkBase = 0.26f, nodes = 5,
                        sheathGap = 0.05f, sheathBase = 0.45f, terminalCover = 0.45f,
                        maxGeneration = 2, forkCount = 2, childLength = 0.8f, forkAngle = new Vector2(15, 35),
                        lateralMin = 2, lateralMax = 4, lateralLength = new Vector2(0.1f, 0.4f),
                        petalsPerRosette = 9, petalLength = 0.95f, petalWidth = 0.3f,
                        shards = 6, shardLength = new Vector2(0.4f, 1f), shardRadius = new Vector2(0.08f, 0.14f),
                        columns = 6, columnHeight = new Vector2(0.1f, 0.35f), columnRadius = new Vector2(0.18f, 0.34f),
                        columnSplay = new Vector2(4, 14),
                    };
                default:
                    return new StageParams
                    {
                        shootLength = new Vector2(3.8f, 4.4f), lean = 0.05f, trunkBase = 0.3f, nodes = 5,
                        sheathGap = 0.08f, sheathBase = 1.2f, terminalCover = 1f,
                        maxGeneration = 2, forkCount = 2, childLength = 0.8f, forkAngle = new Vector2(10, 25),
                        lateralMin = 0, lateralMax = 0, lateralLength = new Vector2(0.1f, 0.4f),
                        petalsPerRosette = 0,
                        shards = 8, shardLength = new Vector2(0.8f, 2f), shardRadius = new Vector2(0.15f, 0.3f),
                        columns = 9, columnHeight = new Vector2(0.3f, 0.95f), columnRadius = new Vector2(0.35f, 0.7f),
                        columnSplay = new Vector2(3, 10),
                    };
            }
        }

        [MenuItem("Terrain/Trees/Build Cyan Tree Greybox")]
        public static void Build()
        {
            EnsureFolder();
            var materials = new[]
            {
                GetMaterial("Trunk", new Color(0.18f, 0.12f, 0.09f), 0.2f),
                GetMaterial("Crystal", new Color(0.1f, 0.8f, 0.85f), 0.9f, MakeCrystal),
                GetMaterial("Petal", new Color(0.85f, 0.45f, 0.15f), 0.4f, MakePetal),
            };

            UseCrystalGraph(materials[1]);

            foreach (Stage stage in System.Enum.GetValues(typeof(Stage)))
            {
                for (var variant = 0; variant < Variants; variant++) BuildPrefab(stage, variant, materials);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Cyan tree greybox built in {Folder}");
        }

        private static void BuildPrefab(Stage stage, int variant, Material[] materials)
        {
            var name = $"CyanTree_{stage}_{variant}";
            var mesh = BuildMesh(stage, variant, name, out var colliderRadius, out var treeHeight);
            mesh = SaveMesh(mesh, $"{Folder}/{name}_Mesh.asset");

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = colliderRadius;
            capsule.height = treeHeight * 0.9f;
            capsule.center = new Vector3(0, treeHeight * 0.45f, 0);
            PrefabUtility.SaveAsPrefabAsset(go, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        private static Mesh BuildMesh(Stage stage, int variant, string name, out float colliderRadius, out float treeHeight)
        {
            var p = StageOf(stage);
            var rng = new Random((int) stage * 100 + variant * 7 + 1);
            // Age of the tree for the crystal shader: old crystal is cloudy; a little different from variant to variant
            var age = Mathf.Clamp01((stage == Stage.Young ? 0.1f : stage == Stage.Mature ? 0.5f : 0.95f) + (variant - 1) * 0.05f);
            var grower = new Grower(p, rng, age, stage == Stage.Young ? 0.035f : stage == Stage.Mature ? 0.055f : 0.09f);

            var length = Range(rng, p.shootLength.x, p.shootLength.y);
            var lean = HorizontalDirection(Range(rng, 0, Mathf.PI * 2)) * p.lean;
            grower.Shoot(Vector3.zero, (Vector3.up + lean).normalized, length, p.trunkBase, 0, p.sheathBase);
            grower.Foot();

            colliderRadius = p.trunkBase + p.sheathGap + p.sheathBase;
            treeHeight = grower.Height;
            return grower.Mesh(name);
        }

        /// <summary>Grows the tree shoot by shoot into a mesh builder.</summary>
        private class Grower
        {
            private const int Segments = 12;
            private readonly StageParams p;
            private readonly Random rng;
            private readonly MeshBuilder builder;

            public float Height { get; private set; }

            public Grower(StageParams p, Random rng, float age, float crystalCell)
            {
                this.p = p;
                this.rng = rng;
                builder = new MeshBuilder(rng, age, crystalCell);
            }

            public Mesh Mesh(string name) => builder.ToMesh(name);

            /// <summary>
            /// A shoot: a segmented stalk with a crystal coat that ends in a broken tip, lateral shoots with small buds at
            /// its base and, at its end, either a bud (the last generation) or forks.
            /// </summary>
            public void Shoot(Vector3 origin, Vector3 direction, float length, float radius, int generation, float thickness)
            {
                var side = Perpendicular(direction, Range(rng, 0, Mathf.PI * 2));
                var bend = side * (length * Range(rng, 0.02f, 0.08f)) + Vector3.up * (length * 0.06f);
                var points = Curve(origin, direction, bend, length, Segments);
                float Radius(float u) => radius * Mathf.Lerp(1f, 0.6f, u) * (1 + 0.22f * Pulse(u, p.nodes));
                builder.Tube(TrunkSub, points, Radii(Segments, Radius), 8, false, Vector2.one, Vector3.zero, false, 0);
                Height = Mathf.Max(Height, points[Segments].y);

                var endThickness = Coat(points, Radius, generation, thickness);
                Laterals(points, direction, length, radius);

                var tip = points[Segments];
                var tipDirection = Tangent(points, Segments);
                if (generation >= p.maxGeneration)
                {
                    if (p.petalsPerRosette > 0)
                    {
                        builder.Rosette(tip, tipDirection, BudScale(generation), p, rng);
                        Height = Mathf.Max(Height, tip.y + p.petalLength * 0.5f);
                    }

                    return;
                }

                for (var k = 0; k < p.forkCount; k++)
                {
                    var angle = Range(rng, p.forkAngle.x, p.forkAngle.y) * Mathf.Deg2Rad;
                    var around = Perpendicular(tipDirection, (k + Range(rng, -0.2f, 0.2f)) / p.forkCount * Mathf.PI * 2);
                    var childDirection = (tipDirection * Mathf.Cos(angle) + around * Mathf.Sin(angle) + Vector3.up * 0.15f).normalized;
                    var childLength = length * p.childLength * Range(rng, 0.85f, 1.1f);
                    Shoot(tip, childDirection, childLength, radius * 0.7f, generation + 1, endThickness * 0.85f);
                }
            }

            /// <summary>Crystals around the foot of the tree: straight columns splayed out of the ground.</summary>
            public void Foot()
            {
                var footRadius = p.trunkBase + p.sheathBase * 0.5f;
                for (var k = 0; k < p.columns; k++)
                {
                    var azimuth = (k + Range(rng, -0.25f, 0.25f)) / p.columns * Mathf.PI * 2;
                    var outward = HorizontalDirection(azimuth);
                    var splay = Range(rng, p.columnSplay.x, p.columnSplay.y) * Mathf.Deg2Rad;
                    var direction = Vector3.up * Mathf.Cos(splay) + outward * Mathf.Sin(splay);
                    var origin = outward * (footRadius * Range(rng, 0.6f, 1.1f)) + Vector3.down * 0.3f;
                    var length = Height * Range(rng, p.columnHeight.x, p.columnHeight.y);
                    builder.Crystal(origin, direction, length, Range(rng, p.columnRadius.x, p.columnRadius.y), true);
                }

                // Shards rise from the lower part of the first shoot, pointing up
                for (var k = 0; k < p.shards; k++)
                {
                    var azimuth = Range(rng, 0, Mathf.PI * 2);
                    var splay = Range(rng, 8, 30) * Mathf.Deg2Rad;
                    var outward = HorizontalDirection(azimuth);
                    var direction = Vector3.up * Mathf.Cos(splay) + outward * Mathf.Sin(splay);
                    var origin = outward * (p.trunkBase + p.sheathBase * 0.7f) + Vector3.up * Range(rng, 0.1f, Height * 0.3f);
                    builder.Crystal(origin, direction, Range(rng, p.shardLength.x, p.shardLength.y),
                        Range(rng, p.shardRadius.x, p.shardRadius.y), false);
                }
            }

            /// <summary>
            /// The crystal grows without a break from the foot: a shoot that has flowered and forked is coated to its end,
            /// where a collar of crystal grew outward while it flowered. Only the youngest shoots are coated partway. The
            /// coat of a child starts as thick as its parent's ended, so the crystal flows from one shoot to the next.
            /// Returns the thickness of the coat at the end of the shoot.
            /// </summary>
            private float Coat(Vector3[] points, System.Func<float, float> radius, int generation, float thickness)
            {
                var forks = generation < p.maxGeneration;
                var top = forks ? Segments : Mathf.Max(2, Mathf.CeilToInt(p.terminalCover * Segments));
                var endThickness = thickness * 0.7f;
                var coatPoints = new Vector3[top + 1];
                System.Array.Copy(points, coatPoints, top + 1);
                // Even thickness, only a little thinner towards the end: straight, not carrot-like
                var coatRadii = Radii(top, u => (radius(u * top / Segments) + p.sheathGap
                                                 + Mathf.Lerp(thickness, endThickness, u * top / Segments))
                                                * (1 + 0.12f * ((float) rng.NextDouble() - 0.5f)));
                builder.CrystalTube(coatPoints, coatRadii, !forks);

                var end = Tangent(coatPoints, top);
                if (forks) builder.Collar(coatPoints[top], end, coatRadii[top]);
                else if (top < Segments) builder.Bush(coatPoints[top], end, coatRadii[top]);
                return endThickness;
            }

            /// <summary>Small shoots at the base of a shoot, spreading out sideways; each ends in a smaller bud.</summary>
            private void Laterals(Vector3[] points, Vector3 direction, float length, float radius)
            {
                var count = rng.Next(p.lateralMin, p.lateralMax + 1);
                for (var k = 0; k < count; k++)
                {
                    var origin = Sample(points, Range(rng, 0.02f, 0.1f));
                    var azimuth = (k + Range(rng, -0.2f, 0.2f)) / count * Mathf.PI * 2 + 0.7f;
                    var tilt = Range(rng, 55, 85) * Mathf.Deg2Rad;
                    var radial = Perpendicular(direction, azimuth);
                    var lateralDirection = (direction * Mathf.Cos(tilt) + radial * Mathf.Sin(tilt)).normalized;
                    var lateralLength = length * Range(rng, p.lateralLength.x, p.lateralLength.y);
                    var lateral = Curve(origin, lateralDirection, Vector3.up * (lateralLength * 0.35f), lateralLength, 6);
                    var start = radius * 0.45f;
                    builder.Tube(TrunkSub, lateral, Radii(6, u => Mathf.Lerp(start, 0.012f, u)), 6, false, Vector2.one,
                        Vector3.zero, false, 0);
                    if (p.petalsPerRosette > 0)
                    {
                        builder.Rosette(lateral[6], Tangent(lateral, 6), BudScale(p.maxGeneration) * Range(rng, 0.65f, 0.85f), p, rng);
                    }
                }
            }

            private float BudScale(int generation) => Mathf.Lerp(1f, 0.7f, generation / (float) Mathf.Max(1, p.maxGeneration));
        }

        /// <summary>A stable pseudo-random value 0..1 of a point.</summary>
        private static float Hash01(Vector3 point) =>
            Mathf.Repeat(Mathf.Sin(Vector3.Dot(point, new Vector3(12.9898f, 78.233f, 37.719f))) * 43758.547f, 1f);

        /// <summary>1 at the nodes of a stalk, 0 between them.</summary>
        private static float Pulse(float u, int nodes) => Mathf.Pow(Mathf.Max(0, Mathf.Cos(Mathf.PI * 2 * nodes * u)), 6);

        private static float Range(Random rng, float min, float max) => min + (float) rng.NextDouble() * (max - min);

        private static Vector3 HorizontalDirection(float azimuth) => new Vector3(Mathf.Cos(azimuth), 0, Mathf.Sin(azimuth));

        /// <summary>A unit vector perpendicular to the axis, turned by the angle around it.</summary>
        private static Vector3 Perpendicular(Vector3 axis, float angle)
        {
            axis.Normalize();
            var a = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            var b = Vector3.Cross(axis, a);
            return a * Mathf.Cos(angle) + b * Mathf.Sin(angle);
        }

        private static Vector3[] Curve(Vector3 start, Vector3 direction, Vector3 bend, float length, int segments)
        {
            var points = new Vector3[segments + 1];
            direction.Normalize();
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float) segments;
                points[i] = start + direction * (length * t) + bend * (t * t);
            }

            return points;
        }

        private static float[] Radii(int segments, System.Func<float, float> radius)
        {
            var radii = new float[segments + 1];
            for (var i = 0; i <= segments; i++) radii[i] = radius(i / (float) segments);
            return radii;
        }

        private static Vector3 Sample(Vector3[] points, float t)
        {
            var scaled = Mathf.Clamp01(t) * (points.Length - 1);
            var index = Mathf.Min((int) scaled, points.Length - 2);
            return Vector3.Lerp(points[index], points[index + 1], scaled - index);
        }

        private static Vector3 Tangent(Vector3[] points, int index) =>
            (points[index] - points[Mathf.Max(index - 1, 0)]).normalized;

        private class MeshBuilder
        {
            private readonly List<Vector3> vertices = new ();
            private readonly List<Vector3> normals = new ();
            private readonly List<Vector2> uvs = new ();
            private readonly List<Color> colors = new ();
            private readonly List<int>[] triangles = { new (), new (), new () };
            private readonly Random rng;
            private readonly float age;
            private readonly float crystalCell;
            private readonly CrystalField crystals = new ();

            /// <summary>
            /// age: 0 young .. 1 old, goes into the vertex colors of the crystal (CyanCrystal.hlsl). crystalCell: size of
            /// the grid cells the crystal is meshed on, meters; the smallest crystals must be a few of them wide.
            /// </summary>
            public MeshBuilder(Random rng, float age, float crystalCell)
            {
                this.rng = rng;
                this.age = age;
                this.crystalCell = crystalCell;
            }

            /// <summary>
            /// A tube along points with a radius at each of them. Flat: every face has its own normal (crystal).
            /// squash scales the cross section along its first and second axis; hint is the direction of the first.
            /// Both ends are closed unless they taper to a point; capStart closes the start too. tipJitter breaks the
            /// end: its corners are pushed along the tube by up to that many radii.
            /// </summary>
            public void Tube(int sub, Vector3[] points, float[] radii, int sides, bool flat, Vector2 squash, Vector3 hint,
                bool capStart, float tipJitter)
            {
                var count = points.Length;
                var rings = new Vector3[count][];

                // Vertex colors of the crystal, read by CyanCrystal.hlsl: r its own random value, g along it from the
                // foot to the tip, b the age of the tree, a thickness. Everything else doesn't use them
                var crystalId = Hash01(points[0]);
                Color Paint(int ring) => sub != CrystalSub
                    ? Color.clear
                    : new Color(crystalId, ring / (float) Mathf.Max(1, count - 1), age, Mathf.Clamp01(radii[ring] / 0.9f));
                var reference = hint != Vector3.zero ? hint : Mathf.Abs(Tangent(points, 1).y) > 0.9f ? Vector3.right : Vector3.up;
                var u = reference;
                for (var i = 0; i < count; i++)
                {
                    var tangent = (points[Mathf.Min(i + 1, count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                    u = (u - tangent * Vector3.Dot(u, tangent)).normalized;
                    var v = Vector3.Cross(tangent, u);
                    rings[i] = new Vector3[sides];
                    for (var s = 0; s < sides; s++)
                    {
                        var angle = Mathf.PI * 2 * s / sides;
                        rings[i][s] = points[i] + (u * (Mathf.Cos(angle) * squash.x) + v * (Mathf.Sin(angle) * squash.y)) * radii[i];
                    }
                }

                var last = count - 1;
                var lastTangent = Tangent(points, last);
                var capCenter = points[last];
                if (tipJitter > 0)
                {
                    for (var s = 0; s < sides; s++) rings[last][s] += lastTangent * (Range(rng, -0.3f, 1f) * tipJitter * radii[last]);
                    capCenter += lastTangent * (tipJitter * radii[last] * 0.35f);
                }

                var smoothBase = vertices.Count;
                if (!flat)
                {
                    for (var i = 0; i < count; i++)
                    {
                        for (var s = 0; s < sides; s++)
                        {
                            vertices.Add(rings[i][s]);
                            normals.Add((rings[i][s] - points[i]).normalized);
                            uvs.Add(new Vector2(s / (float) sides, i / (float) (count - 1)));
                            colors.Add(Color.clear);
                        }
                    }
                }

                var list = triangles[sub];
                for (var i = 0; i < count - 1; i++)
                {
                    for (var s = 0; s < sides; s++)
                    {
                        var next = (s + 1) % sides;
                        if (flat)
                        {
                            AddFlat(list, rings[i][s], rings[i][next], rings[i + 1][s], Paint(i), Paint(i), Paint(i + 1));
                            AddFlat(list, rings[i][next], rings[i + 1][next], rings[i + 1][s], Paint(i), Paint(i + 1), Paint(i + 1));
                        }
                        else
                        {
                            var a = smoothBase + i * sides + s;
                            var b = smoothBase + i * sides + next;
                            var c = smoothBase + (i + 1) * sides + s;
                            var d = smoothBase + (i + 1) * sides + next;
                            list.Add(a); list.Add(b); list.Add(c);
                            list.Add(b); list.Add(d); list.Add(c);
                        }
                    }
                }

                for (var s = 0; s < sides; s++)
                {
                    var next = (s + 1) % sides;
                    if (radii[last] > 0.001f) AddFlat(list, capCenter, rings[last][s], rings[last][next], Paint(last), Paint(last), Paint(last));
                    if (capStart && radii[0] > 0.001f) AddFlat(list, points[0], rings[0][next], rings[0][s], Paint(0), Paint(0), Paint(0));
                }
            }

            /// <summary>A crystal: a straight hexagonal column with a broken tip, optionally with a bush of small ones on it.</summary>
            public void Crystal(Vector3 origin, Vector3 direction, float length, float radius, bool bush)
            {
                direction.Normalize();
                var top = origin + direction * length;
                CrystalTube(new[] { origin, top }, new[] { radius, radius * 0.95f }, true);
                if (bush) Bush(top, direction, radius);
            }

            /// <summary>
            /// A chain of crystal prisms along points; it is one solid with all the other crystals (CrystalField), meshed
            /// when the mesh is made. The last end is broken when asked.
            /// </summary>
            public void CrystalTube(Vector3[] points, float[] radii, bool brokenTip)
            {
                if (SolidCrystal)
                {
                    crystals.AddTube(points, radii, Hash01(points[0]), rng, brokenTip);
                    return;
                }

                // Plain prisms with a jagged tip: few vertices. The crystal is opaque, so shells inside each other don't show
                Tube(CrystalSub, points, radii, 6, true, Vector2.one, Vector3.zero, true, brokenTip ? 0.6f : 0);
            }

            /// <summary>
            /// The node where a shoot flowered and forked: the crystal grew outward there, a swelling with a few short
            /// crystals leaning out of it.
            /// </summary>
            public void Collar(Vector3 center, Vector3 axis, float radius)
            {
                axis.Normalize();
                var half = radius * 0.6f;
                CrystalTube(new[] { center - axis * half, center + axis * half }, new[] { radius * 1.18f, radius * 1.18f }, false);

                var count = rng.Next(3, 6);
                for (var k = 0; k < count; k++)
                {
                    var azimuth = Mathf.PI * 2 * (k + Range(rng, -0.3f, 0.3f)) / count;
                    var outward = Perpendicular(axis, azimuth);
                    var tilt = Range(rng, 25, 55) * Mathf.Deg2Rad;
                    var direction = axis * Mathf.Cos(tilt) + outward * Mathf.Sin(tilt);
                    var origin = center + outward * (radius * 0.8f) - axis * (radius * 0.2f);
                    var small = radius * Range(rng, 0.2f, 0.35f);
                    var length = radius * Range(rng, 0.7f, 1.4f) + 0.08f;
                    CrystalTube(new[] { origin, origin + direction * length }, new[] { small, small * 0.9f }, true);
                }
            }

            /// <summary>A chaotic bush of small crystals crowding around a point, leaning away from the axis.</summary>
            public void Bush(Vector3 center, Vector3 axis, float radius)
            {
                var count = rng.Next(3, 6);
                for (var k = 0; k < count; k++)
                {
                    var offset = Perpendicular(axis, Range(rng, 0, Mathf.PI * 2)) * (radius * Range(rng, 0, 0.6f));
                    var direction = (axis + Perpendicular(axis, Range(rng, 0, Mathf.PI * 2)) * Range(rng, 0.1f, 0.6f)).normalized;
                    var length = radius * Range(rng, 0.5f, 1.5f) + 0.05f;
                    var small = radius * Range(rng, 0.18f, 0.35f);
                    CrystalTube(new[] { center + offset, center + offset + direction * length }, new[] { small, small * 0.9f }, true);
                }
            }

            /// <summary>
            /// A bud opened into a succulent rosette: fleshy petals in rings, spreading far out sideways, the inner rings
            /// stand more upright and are smaller.
            /// </summary>
            public void Rosette(Vector3 tip, Vector3 axis, float scale, StageParams stage, Random random)
            {
                axis.Normalize();
                var count = stage.petalsPerRosette;
                var length = stage.petalLength * scale;
                var width = stage.petalWidth * scale;
                Ring(tip, axis, count, length, width, 50, 85, random, 0);
                Ring(tip, axis, Mathf.Max(3, count * 2 / 3), length * 0.75f, width * 0.85f, 25, 55, random, 0.5f);
                Ring(tip, axis, 3, length * 0.45f, width * 0.7f, 5, 20, random, 0.25f);
            }

            private void Ring(Vector3 tip, Vector3 axis, int count, float length, float width, float minTilt, float maxTilt,
                Random random, float phase)
            {
                for (var i = 0; i < count; i++)
                {
                    var azimuth = Mathf.PI * 2 * (i + phase) / count + Range(random, -0.15f, 0.15f);
                    var tilt = Range(random, minTilt, maxTilt) * Mathf.Deg2Rad;
                    var radial = Perpendicular(axis, azimuth);
                    var tangential = Vector3.Cross(axis, radial);
                    var direction = axis * Mathf.Cos(tilt) + radial * Mathf.Sin(tilt);
                    var petalLength = length * Range(random, 0.85f, 1.15f);
                    var points = Curve(tip, direction, axis * (petalLength * 0.25f), petalLength, 7);
                    var radii = Radii(7, t => width * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f)), 0.8f));
                    Tube(PetalSub, points, radii, 8, false, new Vector2(1, 0.35f), tangential, false, 0);
                }
            }

            public Mesh ToMesh(string name)
            {
                // The crystal is meshed last, as one solid, below the ground cut off
                crystals.Build(crystalCell, -0.12f, age, vertices, normals, uvs, colors, triangles[CrystalSub]);
                // Degenerate geometry (zero-length segments, normalizing a zero vector) leaves NaN vertices, which make the
                // bounds invalid: such vertices are put at the origin and the triangles that use them are dropped
                var broken = new bool[vertices.Count];
                var brokenCount = 0;
                for (var i = 0; i < vertices.Count; i++)
                {
                    var v = vertices[i];
                    if (IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z)) continue;
                    broken[i] = true;
                    brokenCount++;
                    vertices[i] = Vector3.zero;
                }
                for (var i = 0; i < normals.Count; i++)
                {
                    var n = normals[i];
                    if (!IsFinite(n.x) || !IsFinite(n.y) || !IsFinite(n.z)) normals[i] = Vector3.up;
                }
                if (brokenCount > 0)
                {
                    Debug.LogWarning($"CyanTreeGreybox: {name} had {brokenCount} invalid vertices, their triangles were dropped");
                    foreach (var list in triangles)
                    {
                        for (var t = list.Count - 3; t >= 0; t -= 3)
                        {
                            if (broken[list[t]] || broken[list[t + 1]] || broken[list[t + 2]]) list.RemoveRange(t, 3);
                        }
                    }
                }
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                mesh.subMeshCount = triangles.Length;
                for (var i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
                mesh.RecalculateBounds();
                return mesh;
            }

            private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

            private void AddFlat(List<int> list, Vector3 a, Vector3 b, Vector3 c, Color colorA, Color colorB, Color colorC)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                var index = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                colors.Add(colorA); colors.Add(colorB); colors.Add(colorC);
                normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 1));
                list.Add(index); list.Add(index + 1); list.Add(index + 2);
            }
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Folder)) return;
            AssetDatabase.CreateFolder("Assets/_game/Prefabs/Trees", "CyanGreybox");
        }

        /// <summary>Writes the mesh into the asset of that path, or creates it; an existing asset keeps its guid.</summary>
        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>
        /// Greybox material; made once, so what is tuned in the Editor stays. setup shapes it further the first time
        /// (a material still marked as made by an earlier version of the greybox is set up again).
        /// </summary>
        private static Material GetMaterial(string name, Color color, float smoothness, System.Func<Material, bool> setup = null)
        {
            var path = $"{Folder}/CyanGreybox_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var material = existing;
            if (material == null)
            {
                var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = $"CyanGreybox_{name}" };
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(material, path);
            }

            if (setup != null && material.HasProperty("_SurfaceType") && setup(material))
            {
                HDMaterial.ValidateMaterial(material);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private const string CrystalShaderPath = "Assets/_game/Visual/Trees/CyanCrystal.shadergraph";

        /// <summary>
        /// The crystal material uses the CyanCrystal shader graph once it exists (semi-opaque, cloudy with age, reads the
        /// vertex colors of the meshes); until then it stays on the HDRP Lit glass below.
        /// </summary>
        private static void UseCrystalGraph(Material material)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(CrystalShaderPath);
            if (shader == null) return;

            var changed = false;
            if (material.shader != shader)
            {
                material.shader = shader;
                HDMaterial.ValidateMaterial(material);
                changed = true;
            }

            // The crystal is opaque (translucent material): a material made when it was transparent is turned around
            if (material.HasProperty("_SurfaceType") && material.GetFloat("_SurfaceType") > 0.5f)
            {
                material.SetFloat("_SurfaceType", 0);
                material.SetFloat("_DoubleSidedEnable", 0);
                material.SetFloat("_TransparentBackfaceEnable", 0);
                material.SetFloat("_RefractionModel", 0);
                material.SetFloat("_ZWrite", 1);
                material.SetFloat("_RenderQueueType", 1);
                material.renderQueue = -1;
                HDMaterial.ValidateMaterial(material);
                changed = true;
            }

            // Textures of the surface (Visual/Trees/Textures); what is assigned by hand is left alone
            const string textures = "Assets/_game/Visual/Trees/Textures/";
            changed |= AssignTexture(material, "_Cracks", textures + "Noices/Cracks/Cracks 3 - 512x512.png"); // deep cracks
            changed |= AssignTexture(material, "_SurfaceCracks", textures + "Noices/Cracks/Cracks 3 - 512x512.png");
            changed |= AssignTexture(material, "_Milky", textures + "Noices/Milky/Milky 3 - 512x512.png");
            changed |= AssignTexture(material, "_IceColor", textures + "Ice/ice_0002_color_2k.jpg");
            changed |= AssignTexture(material, "_IceNormal", textures + "Ice/ice_0002_normal_directx_2k.png");
            changed |= AssignTexture(material, "_IceRoughness", textures + "Ice/ice_0002_roughness_2k.jpg");
            if (changed) EditorUtility.SetDirty(material);
        }

        private static bool AssignTexture(Material material, string property, string path)
        {
            if (!material.HasProperty(property) || material.GetTexture(property) != null) return false;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return false;

            material.SetTexture(property, texture);
            return true;
        }

        /// <summary>Cyan glass: transparent, thin refraction, both faces, so the dark trunk shows through.</summary>
        private static bool MakeCrystal(Material material)
        {
            if (material.GetFloat("_SurfaceType") > 0.5f) return false; // set up already, tuned by hand since

            material.SetColor("_BaseColor", new Color(0.1f, 0.75f, 0.85f, 0.45f));
            material.SetFloat("_Smoothness", 0.95f);
            material.SetFloat("_SurfaceType", 1);
            material.SetFloat("_BlendMode", 0);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_TransparentZWrite", 0);
            material.SetFloat("_DoubleSidedEnable", 1);
            material.SetFloat("_TransparentBackfaceEnable", 1);
            material.SetFloat("_RefractionModel", 3);
            material.SetFloat("_Ior", 1.5f);
            material.SetFloat("_Thickness", 0.3f);
            material.SetColor("_TransmittanceColor", new Color(0.35f, 0.9f, 0.95f));
            material.SetFloat("_ATDistance", 0.6f);
            material.SetColor("_EmissiveColor", new Color(0.02f, 0.25f, 0.3f));
            return true;
        }

        /// <summary>Petals: glossy and a little glowing, as if light came through them.</summary>
        private static bool MakePetal(Material material)
        {
            if (material.GetFloat("_DoubleSidedEnable") > 0.5f) return false;

            material.SetColor("_BaseColor", new Color(0.8f, 0.3f, 0.12f));
            material.SetFloat("_Smoothness", 0.55f);
            material.SetFloat("_DoubleSidedEnable", 1);
            material.SetColor("_EmissiveColor", new Color(0.18f, 0.05f, 0.01f));
            return true;
        }
    }
}
