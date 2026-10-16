using System.Collections.Generic;
using UnityEngine;

namespace Core.Scatter
{
    public enum ScatterAreaShape
    {
        /// <summary>Box of <see cref="ScatterAreaData.Size"/> centered on the area origin.</summary>
        Box = 0,
        /// <summary>Sphere of radius <see cref="ScatterAreaData.Size"/>.x around the area origin.</summary>
        Sphere = 1,
        /// <summary>
        /// Prism over <see cref="ScatterAreaData.Polygon"/> (local x, z), from local y = Size.y to y = Size.z.
        /// </summary>
        Polygon = 2,
    }

    /// <summary>An area kept clear of scattered details (grass, small stones), as it is this frame.</summary>
    public struct ScatterAreaData
    {
        public ScatterAreaShape Shape;
        /// <summary>Scene space to the space of the area, unscaled: sizes are in meters.</summary>
        public Matrix4x4 WorldToLocal;
        public Vector3 Size;
        /// <summary>Meters outside the area over which the details grow back to their full size.</summary>
        public float Edge;
        /// <summary>Points of a polygon area in local x, z, in order around it.</summary>
        public IReadOnlyList<Vector2> Polygon;
    }

    /// <summary>An area kept clear of scattered details. Read by <see cref="IScatter"/> every frame while it is added.</summary>
    public interface IScatterArea
    {
        ScatterAreaData Area { get; }
    }
}
