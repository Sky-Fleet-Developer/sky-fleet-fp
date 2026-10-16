using UnityEngine;

namespace Core.Grass
{
    /// <summary>A sphere that pushes the grass out of it, as it is this frame.</summary>
    public struct GrassPusherData
    {
        /// <summary>Scene position.</summary>
        public Vector3 Center;
        public float Radius;
        /// <summary>Lean of the grass at the center, in plant heights: about 1 lays it down.</summary>
        public float Strength;
    }

    /// <summary>A sphere pushing the grass. Read by <see cref="IGrass"/> every frame while it is added.</summary>
    public interface IGrassPusher
    {
        GrassPusherData Pusher { get; }
    }
}
