using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameEditor.Trees
{
    /// <summary>
    /// The lower levels of detail of the Cyan Tree, made from the same <see cref="TreeDescription"/> as the full model,
    /// never from its mesh: whatever LOD0 grows into (chips, cracks, more branches), these only pick parts of the
    /// description and draw them in a simple way.
    /// LOD1: no chips or cracks; crystals are hexagonal prisms without a bottom cap, some of them left out; a low
    /// resolution trunk; fewer, low resolution petals, but every flower of the full model is there.
    /// LOD2: the same, with fewer details still, and each flower is one solid without single petals.
    /// </summary>
    public static partial class CyanTreeGreybox
    {
        // LOD1: segments along a shoot's trunk and sides of it, segments of a coat, every n-th shard is kept
        private const int Lod1TrunkSides = 5;
        private const int Lod1TrunkSegments = 4;
        private const int Lod1LateralSegments = 2;
        private const int Lod1CoatSegments = 1;
        private const int Lod1ShardEvery = 2;
        private const int Lod1PetalSegments = 4;
        private const int Lod1PetalSides = 4;

        // LOD2
        private const int Lod2TrunkSides = 4;
        private const int Lod2TrunkSegments = 2;
        private const int Lod2CoatSegments = 1;
        private const int Lod2FlowerSides = 12;

        private const string Lod1CrystalPath = Folder + "/CyanLod1_Crystal.mat";
        private const string Lod1PetalPath = Folder + "/CyanLod1_Petal.mat";

        /// <summary>
        /// Materials of LOD1: the trunk of the full model and the LOD1 shader graph for crystals and petals; until the
        /// graph is imported (Terrain/Trees/Create Cyan LOD1 Materials) the materials of the full model stand in.
        /// </summary>
        private static Material[] Lod1Materials(Material[] full)
        {
            CyanLod1Materials.EnsureCreated();
            var crystal = AssetDatabase.LoadAssetAtPath<Material>(Lod1CrystalPath);
            var petal = AssetDatabase.LoadAssetAtPath<Material>(Lod1PetalPath);
            return new[] { full[TrunkSub], crystal ? crystal : full[CrystalSub], petal ? petal : full[PetalSub] };
        }

        /// <summary>
        /// Materials of LOD2: plain lit colors, made once; swap the shader to Unlit by hand to see which one is better.
        /// </summary>
        private static Material[] Lod2Materials() => new[]
        {
            GetMaterial("Lod2_Trunk", new Color(0.18f, 0.12f, 0.09f), 0.1f),
            GetMaterial("Lod2_Crystal", new Color(0.35f, 0.85f, 0.9f), 0.7f),
            GetMaterial("Lod2_Petal", new Color(0.8f, 0.35f, 0.15f), 0.3f),
        };

        private static Mesh BuildLod1(TreeDescription description, string name)
        {
            var builder = new MeshBuilder(new Random(description.seed), description.age, description.crystalCell);

            // Small shoots stay: every flower of the full model has its place here, and one has to hang on something
            foreach (var stalk in description.stalks)
            {
                Reduce(stalk.points, stalk.radii, stalk.lateral ? Lod1LateralSegments : Lod1TrunkSegments, out var points, out var radii);
                builder.Tube(TrunkSub, points, radii, Lod1TrunkSides, false, Vector2.one, Vector3.zero, false, null);
            }

            var shards = 0;
            foreach (var crystal in description.crystals)
            {
                int segments;
                switch (crystal.kind)
                {
                    case CrystalKind.Coat:
                        segments = Lod1CoatSegments;
                        break;
                    case CrystalKind.Column:
                    case CrystalKind.Collar:
                        segments = 1;
                        break;
                    case CrystalKind.Shard:
                        if (shards++ % Lod1ShardEvery != 0) continue;
                        segments = 1;
                        break;
                    default:
                        continue;
                }

                AddPrism(builder, crystal, segments);
            }

            foreach (var rosette in description.rosettes)
            {
                var middle = 0;
                foreach (var petal in rosette.petals)
                {
                    // The outer ring in full, every other petal of the middle ring, no inner ring
                    if (petal.ring == 2 || (petal.ring == 1 && middle++ % 2 != 0)) continue;
                    Reduce(Curve(petal.tip, petal.direction, petal.axis * (petal.length * 0.25f), petal.length, 9),
                        Radii(9, t => petal.width * PetalProfile(t)), Lod1PetalSegments, out var points, out var radii);
                    builder.Tube(PetalSub, points, radii, Lod1PetalSides, false, new Vector2(1, 0.35f), petal.tangential, false, null);
                }
            }

            return builder.ToMesh(name);
        }

        private static Mesh BuildLod2(TreeDescription description, string name)
        {
            var builder = new MeshBuilder(new Random(description.seed), description.age, description.crystalCell);

            foreach (var stalk in description.stalks)
            {
                Reduce(stalk.points, stalk.radii, stalk.lateral ? 1 : Lod2TrunkSegments, out var points, out var radii);
                builder.Tube(TrunkSub, points, radii, Lod2TrunkSides, false, Vector2.one, Vector3.zero, false, null);
            }

            foreach (var crystal in description.crystals)
            {
                if (crystal.kind == CrystalKind.Coat) AddPrism(builder, crystal, Lod2CoatSegments);
                else if (crystal.kind == CrystalKind.Column) AddPrism(builder, crystal, 1);
            }

            foreach (var rosette in description.rosettes)
            {
                AddFlower(builder, rosette, description.stage.petalLength * rosette.scale);
            }

            return builder.ToMesh(name);
        }

        /// <summary>A crystal as a hexagonal prism chain: a flat end, no cap at the start, none of the trunk inside.</summary>
        private static void AddPrism(MeshBuilder builder, CrystalPart crystal, int segments)
        {
            Reduce(crystal.points, crystal.radii, segments, out var points, out var radii);
            builder.Tube(CrystalSub, points, radii, CrystalSides, true, Vector2.one, Vector3.zero, false, null);
        }

        /// <summary>
        /// A flower as one solid: a shallow bowl with a lobed rim, bud in the middle, closed below. Profile of the
        /// rings, from the middle outwards over the top and back under it: radius and height in petal lengths.
        /// </summary>
        private static readonly Vector2[] FlowerProfile =
        {
            new (0.00f, 0.42f), new (0.22f, 0.38f), new (0.60f, 0.46f), new (0.92f, 0.62f), new (0.55f, 0.10f),
            new (0.00f, 0.00f),
        };

        private static void AddFlower(MeshBuilder builder, RosettePart rosette, float petalLength)
        {
            var sides = Lod2FlowerSides;
            var turn = Quaternion.FromToRotation(Vector3.up, rosette.axis);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            // A ring is a single vertex where the profile reaches the axis
            var ringStart = new int[FlowerProfile.Length];
            var ringSize = new int[FlowerProfile.Length];
            for (var i = 0; i < FlowerProfile.Length; i++)
            {
                var profile = FlowerProfile[i];
                ringStart[i] = vertices.Count;
                ringSize[i] = profile.x < 1e-4f ? 1 : sides;
                for (var k = 0; k < ringSize[i]; k++)
                {
                    var angle = Mathf.PI * 2 * k / sides;
                    // Lobes: the rim is farther out at every other vertex, like the tips of petals
                    var lobe = i == 3 && k % 2 == 1 ? 0.8f : 1f;
                    var radius = profile.x * lobe * petalLength;
                    var local = new Vector3(Mathf.Cos(angle) * radius, profile.y * petalLength, Mathf.Sin(angle) * radius);
                    vertices.Add(rosette.tip + turn * local);
                    uvs.Add(new Vector2(Mathf.Min(profile.x, 1f), 0.5f));
                }
            }

            for (var i = 0; i + 1 < FlowerProfile.Length; i++)
            {
                for (var k = 0; k < sides; k++)
                {
                    var next = (k + 1) % sides;
                    // Rings of one vertex are all the same index
                    int P(int ring, int index) => ringStart[ring] + (ringSize[ring] == 1 ? 0 : index);
                    var pk = P(i, k);
                    var pn = P(i, next);
                    var qk = P(i + 1, k);
                    var qn = P(i + 1, next);
                    if (qk != qn)
                    {
                        triangles.Add(pk); triangles.Add(qn); triangles.Add(qk);
                    }

                    if (pk != pn)
                    {
                        triangles.Add(pk); triangles.Add(pn); triangles.Add(qn);
                    }
                }
            }

            builder.Solid(PetalSub, vertices, uvs, triangles);
        }

        /// <summary>The points and radii of a chain reduced to a number of segments, evenly along it.</summary>
        private static void Reduce(Vector3[] points, float[] radii, int segments, out Vector3[] reducedPoints, out float[] reducedRadii)
        {
            if (points.Length - 1 <= segments)
            {
                reducedPoints = points;
                reducedRadii = radii;
                return;
            }

            reducedPoints = new Vector3[segments + 1];
            reducedRadii = new float[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var scaled = i / (float) segments * (points.Length - 1);
                var index = Mathf.Min((int) scaled, points.Length - 2);
                var t = scaled - index;
                reducedPoints[i] = Vector3.Lerp(points[index], points[index + 1], t);
                reducedRadii[i] = Mathf.Lerp(radii[index], radii[index + 1], t);
            }
        }
    }
}
