using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameEditor.Trees
{
    /// <summary>
    /// The common description of a Cyan Tree: stalks, crystals and rosettes of petals as plain numbers. The Grower
    /// writes it once (all the randomness is spent there), and each level of detail makes its own mesh from it: the
    /// full model keeps every part, the lower levels pick the parts that matter and simplify them.
    /// </summary>
    public static partial class CyanTreeGreybox
    {
        /// <summary>What a crystal of the tree is, so that a lower level of detail can keep the ones that matter.</summary>
        private enum CrystalKind
        {
            /// <summary>The coat of a shoot.</summary>
            Coat,
            /// <summary>A column at the foot of the tree.</summary>
            Column,
            /// <summary>A shard rising from the first shoot.</summary>
            Shard,
            /// <summary>The swelling where a shoot flowered.</summary>
            Collar,
            /// <summary>A short crystal leaning out of a collar.</summary>
            CollarShard,
            /// <summary>A small crystal of a bush at the end of a coat or a column.</summary>
            Bush,
        }

        private const int CrystalSides = 6;
        /// <summary>How far the corners of a broken tip are pushed along the crystal, in radii.</summary>
        private const float BrokenTipJitter = 0.6f;

        private class StalkPart
        {
            public Vector3[] points;
            public float[] radii;
            public int sides;
            public int generation;
            /// <summary>A small shoot at the base of a shoot.</summary>
            public bool lateral;
        }

        private class CrystalPart
        {
            public CrystalKind kind;
            public Vector3[] points;
            public float[] radii;
            /// <summary>Radius of the trunk inside at each point (the crystal shader shows it), or null.</summary>
            public float[] trunkRadii;
            /// <summary>Where each corner of the end lies along the crystal in radii (broken tip), or null for a flat end.</summary>
            public float[] tipOffsets;
            public int generation;
        }

        private class PetalPart
        {
            public Vector3 tip, axis, direction, tangential;
            public float length, width;
            /// <summary>0 outer ring .. 2 inner.</summary>
            public int ring;
        }

        private class RosettePart
        {
            public Vector3 tip, axis;
            public float scale;
            public bool lateral;
            public readonly List<PetalPart> petals = new ();
        }

        private class TreeDescription
        {
            public readonly StageParams stage;
            public readonly int seed;
            /// <summary>0 young .. 1 old.</summary>
            public readonly float age;
            /// <summary>Size of the grid cells of the solid crystal (full model only).</summary>
            public readonly float crystalCell;
            public readonly List<StalkPart> stalks = new ();
            public readonly List<CrystalPart> crystals = new ();
            public readonly List<RosettePart> rosettes = new ();
            public float height;
            public float colliderRadius;
            private readonly Random rng;

            public TreeDescription(StageParams stage, Random rng, int seed, float age, float crystalCell)
            {
                this.stage = stage;
                this.rng = rng;
                this.seed = seed;
                this.age = age;
                this.crystalCell = crystalCell;
            }

            public void AddStalk(Vector3[] points, float[] radii, int sides, int generation, bool lateral) =>
                stalks.Add(new StalkPart { points = points, radii = radii, sides = sides, generation = generation, lateral = lateral });

            public void AddCrystal(CrystalKind kind, Vector3[] points, float[] radii, bool brokenTip, int generation,
                float[] trunkRadii = null)
            {
                float[] tipOffsets = null;
                if (brokenTip)
                {
                    tipOffsets = new float[CrystalSides];
                    for (var s = 0; s < CrystalSides; s++) tipOffsets[s] = Range(rng, -0.3f, 1f);
                }

                crystals.Add(new CrystalPart
                {
                    kind = kind, points = points, radii = radii, trunkRadii = trunkRadii, tipOffsets = tipOffsets,
                    generation = generation,
                });
            }

            /// <summary>A crystal: a straight hexagonal column with a broken tip, optionally with a bush of small ones on it.</summary>
            public void AddColumn(Vector3 origin, Vector3 direction, float length, float radius, bool bush, int generation)
            {
                direction.Normalize();
                var top = origin + direction * length;
                AddCrystal(bush ? CrystalKind.Column : CrystalKind.Shard, new[] { origin, top }, new[] { radius, radius * 0.95f },
                    true, generation);
                if (bush) AddBush(top, direction, radius, generation);
            }

            /// <summary>
            /// The node where a shoot flowered and forked: the crystal grew outward there, a swelling with a few short
            /// crystals leaning out of it.
            /// </summary>
            public void AddCollar(Vector3 center, Vector3 axis, float radius, int generation)
            {
                axis.Normalize();
                var half = radius * 0.6f;
                AddCrystal(CrystalKind.Collar, new[] { center - axis * half, center + axis * half },
                    new[] { radius * 1.18f, radius * 1.18f }, false, generation);

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
                    AddCrystal(CrystalKind.CollarShard, new[] { origin, origin + direction * length }, new[] { small, small * 0.9f },
                        true, generation);
                }
            }

            /// <summary>A chaotic bush of small crystals crowding around a point, leaning away from the axis.</summary>
            public void AddBush(Vector3 center, Vector3 axis, float radius, int generation)
            {
                var count = rng.Next(3, 6);
                for (var k = 0; k < count; k++)
                {
                    var offset = Perpendicular(axis, Range(rng, 0, Mathf.PI * 2)) * (radius * Range(rng, 0, 0.6f));
                    var direction = (axis + Perpendicular(axis, Range(rng, 0, Mathf.PI * 2)) * Range(rng, 0.1f, 0.6f)).normalized;
                    var length = radius * Range(rng, 0.5f, 1.5f) + 0.05f;
                    var small = radius * Range(rng, 0.18f, 0.35f);
                    AddCrystal(CrystalKind.Bush, new[] { center + offset, center + offset + direction * length },
                        new[] { small, small * 0.9f }, true, generation);
                }
            }

            /// <summary>
            /// A bud opened into a succulent rosette: fleshy petals in rings, spreading far out sideways, the inner rings
            /// stand more upright and are smaller.
            /// </summary>
            public void AddRosette(Vector3 tip, Vector3 axis, float scale, bool lateral)
            {
                axis.Normalize();
                var rosette = new RosettePart { tip = tip, axis = axis, scale = scale, lateral = lateral };
                rosettes.Add(rosette);
                var count = stage.petalsPerRosette;
                var length = stage.petalLength * scale;
                var width = stage.petalWidth * scale;
                AddRing(rosette, 0, count, length, width, 50, 85, 0);
                AddRing(rosette, 1, Mathf.Max(3, count * 2 / 3), length * 0.75f, width * 0.85f, 25, 55, 0.5f);
                AddRing(rosette, 2, 3, length * 0.45f, width * 0.7f, 5, 20, 0.25f);
            }

            private void AddRing(RosettePart rosette, int ring, int count, float length, float width, float minTilt, float maxTilt,
                float phase)
            {
                for (var i = 0; i < count; i++)
                {
                    var azimuth = Mathf.PI * 2 * (i + phase) / count + Range(rng, -0.15f, 0.15f);
                    var tilt = Range(rng, minTilt, maxTilt) * Mathf.Deg2Rad;
                    var radial = Perpendicular(rosette.axis, azimuth);
                    var tangential = Vector3.Cross(rosette.axis, radial);
                    var direction = rosette.axis * Mathf.Cos(tilt) + radial * Mathf.Sin(tilt);
                    var petalLength = length * Range(rng, 0.85f, 1.15f);
                    rosette.petals.Add(new PetalPart
                    {
                        tip = rosette.tip, axis = rosette.axis, direction = direction, tangential = tangential,
                        length = petalLength, width = width, ring = ring,
                    });
                }
            }
        }
    }
}
