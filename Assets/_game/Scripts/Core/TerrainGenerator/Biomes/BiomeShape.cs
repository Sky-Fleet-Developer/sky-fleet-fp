using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>Values match BIOME_SHAPE_* in BiomeDiagram.hlsl.</summary>
    public enum BiomeShapeType
    {
        /// <summary>The cell of the power diagram around the site.</summary>
        Cell = 0,
        Rectangle = 1,
        Circle = 2,
        Polygon = 3,
    }

    /// <summary>
    /// Area of a biome. Besides the diagram cell a biome can be bounded by a rectangle, a circle or a polygon.
    /// Shaped biomes are always layers (a Normal blend mode acts as Override) and take no part in the power diagram:
    /// they neither move the cell borders nor are moved by them, so details can be added without changing the structure
    /// of the diagram. The shape lies in the local space of the site, so it moves and turns with it.
    /// <para>
    /// Weight of a shaped biome: 0 outside, then the fade curve over the fade distance from the border inward,
    /// 1 deeper inside.
    /// </para>
    /// </summary>
    [Serializable]
    public class BiomeShape
    {
        /// <summary>Samples of the fade curve uploaded to the GPU. Matches BIOME_CURVE_SAMPLES in BiomeDiagram.hlsl.</summary>
        public const int CurveSamples = 32;

        [SerializeField] private BiomeShapeType type = BiomeShapeType.Cell;
        [PropertyTooltip("Full width (x) and length (z) of the rectangle, in meters")]
        [SerializeField, ShowIf(nameof(IsRectangle)), MinValue(0)] private Vector2 size = new (1000, 1000);
        [PropertyTooltip("Radius of the circle, in meters")]
        [SerializeField, ShowIf(nameof(IsCircle)), MinValue(0)] private float radius = 500;
        [PropertyTooltip("Corners of the polygon around the site, in meters, in the local space of the site. At least 3, in any winding")]
        [SerializeField, ShowIf(nameof(IsPolygon))] private List<Vector2> points = new ()
        {
            new Vector2(-500, -500), new Vector2(-500, 500), new Vector2(500, 500), new Vector2(500, -500),
        };
        [PropertyTooltip("Distance from the border inward, in meters, where the biome reaches 100%. 0 = a hard edge")]
        [SerializeField, ShowIf(nameof(IsShaped)), MinValue(0)] private float fadeDistance = 100;
        [PropertyTooltip("Weight over the fade distance: x = 0 at the border, x = 1 at the fade distance inside")]
        [SerializeField, ShowIf(nameof(IsShaped))] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [PropertyTooltip("Bend the border with the terrain biome edge noise. Off keeps the exact outline")]
        [SerializeField, ShowIf(nameof(IsShaped))] private bool bendByEdgeNoise;

        public BiomeShapeType Type => type;
        public bool IsShaped => type != BiomeShapeType.Cell;
        public Vector2 Size => size;
        public float Radius => radius;
        public IReadOnlyList<Vector2> Points => points;
        public float FadeDistance => Mathf.Max(0, fadeDistance);
        public bool BendByEdgeNoise => bendByEdgeNoise;

        private bool IsRectangle => type == BiomeShapeType.Rectangle;
        private bool IsCircle => type == BiomeShapeType.Circle;
        private bool IsPolygon => type == BiomeShapeType.Polygon;

        /// <summary>False for a polygon with fewer than 3 corners: it has no area.</summary>
        public bool IsValid => type != BiomeShapeType.Polygon || points != null && points.Count >= 3;

        /// <summary>Distance from the site to the farthest point of the shape, in meters.</summary>
        public float GetBoundsRadius()
        {
            switch (type)
            {
                case BiomeShapeType.Rectangle:
                    return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y)).magnitude * 0.5f;
                case BiomeShapeType.Circle:
                    return Mathf.Max(0, radius);
                case BiomeShapeType.Polygon:
                    float farthest = 0;
                    foreach (Vector2 point in points)
                    {
                        farthest = Mathf.Max(farthest, point.magnitude);
                    }
                    return farthest;
                default:
                    return 0;
            }
        }

        /// <summary>The fade curve sampled at <see cref="CurveSamples"/> even steps of 0..1, clamped to 0..1.</summary>
        public void AppendCurveSamples(List<float> result)
        {
            for (int i = 0; i < CurveSamples; i++)
            {
                float value = fadeCurve != null && fadeCurve.length > 0 ? fadeCurve.Evaluate(i / (CurveSamples - 1f)) : i / (CurveSamples - 1f);
                result.Add(Mathf.Clamp01(value));
            }
        }

        /// <summary>Everything the terrain reads from the shape, flattened, to find out when it changed.</summary>
        public void AppendSnapshot(List<Vector4> snapshot)
        {
            snapshot.Add(new Vector4((int)type, size.x, size.y, radius));
            snapshot.Add(new Vector4(fadeDistance, bendByEdgeNoise ? 1 : 0, points?.Count ?? 0, 0));
            if (type == BiomeShapeType.Polygon && points != null)
            {
                foreach (Vector2 point in points)
                {
                    snapshot.Add(new Vector4(point.x, point.y, 0, 0));
                }
            }

            if (type == BiomeShapeType.Cell)
            {
                return;
            }

            for (int i = 0; i < CurveSamples; i += 4)
            {
                snapshot.Add(new Vector4(
                    fadeCurve?.Evaluate(i / (CurveSamples - 1f)) ?? 0, fadeCurve?.Evaluate((i + 1) / (CurveSamples - 1f)) ?? 0,
                    fadeCurve?.Evaluate((i + 2) / (CurveSamples - 1f)) ?? 0, fadeCurve?.Evaluate((i + 3) / (CurveSamples - 1f)) ?? 0));
            }
        }
    }
}
