using System.Collections.Generic;
using UnityEngine;

namespace Core.Grass
{
    public enum GrassAreaShape
    {
        /// <summary>Box of <see cref="GrassAreaData.Size"/> centered on the area origin.</summary>
        Box = 0,
        /// <summary>Sphere of radius <see cref="GrassAreaData.Size"/>.x around the area origin.</summary>
        Sphere = 1,
        /// <summary>
        /// Prism over <see cref="GrassAreaData.Polygon"/> (local x, z), from local y = Size.y to y = Size.z.
        /// </summary>
        Polygon = 2,
    }

    /// <summary>An area without grass, as it is this frame.</summary>
    public struct GrassAreaData
    {
        public GrassAreaShape Shape;
        /// <summary>Scene space to the space of the area, unscaled: sizes are in meters.</summary>
        public Matrix4x4 WorldToLocal;
        public Vector3 Size;
        /// <summary>Meters outside the area over which the grass grows back to its full height.</summary>
        public float Edge;
        /// <summary>Points of a polygon area in local x, z, in order around it.</summary>
        public IReadOnlyList<Vector2> Polygon;
    }

    /// <summary>A sphere that pushes the grass out of it, as it is this frame.</summary>
    public struct GrassPusherData
    {
        /// <summary>Scene position.</summary>
        public Vector3 Center;
        public float Radius;
        /// <summary>Lean of the grass at the center, in plant heights: about 1 lays it down.</summary>
        public float Strength;
    }

    /// <summary>An area without grass. Read by <see cref="IGrass"/> every frame while it is added.</summary>
    public interface IGrassArea
    {
        GrassAreaData Area { get; }
    }

    /// <summary>A sphere pushing the grass. Read by <see cref="IGrass"/> every frame while it is added.</summary>
    public interface IGrassPusher
    {
        GrassPusherData Pusher { get; }
    }
}
