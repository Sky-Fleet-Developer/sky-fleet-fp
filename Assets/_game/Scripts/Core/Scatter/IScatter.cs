using System;
using UnityEngine;

namespace Core.Scatter
{
    /// <summary>
    /// Details scattered over the terrain on the GPU around the camera (grass, small stones). Things shared by every
    /// kind of them.
    /// </summary>
    public interface IScatter
    {
        /// <summary>Keeps every kind of detail out of an area until it is removed. Adding twice adds once.</summary>
        void AddArea(IScatterArea area);

        void RemoveArea(IScatterArea area);
    }

    /// <summary>
    /// The areas of <see cref="IScatter"/> as the GPU sees them, for the placement shaders of the kinds of details.
    /// Binds what Visual/Scatter/ScatterPlacement.hlsl reads: scatter_areas, scatter_area_count, scatter_polygons.
    /// </summary>
    public interface IScatterGpuAreas
    {
        /// <summary>Binds the areas as they are this frame to a kernel. Call it every frame before dispatching.</summary>
        void Bind(ComputeShader shader, int kernel);
    }

    /// <summary>
    /// How a kind of detail uses the ground covered by others (<see cref="IScatterGpuCover"/>). The cover has two
    /// layers: large objects (rocks) cover the ground for everything smaller, small details (stones) for the plants.
    /// A kind reads the layers below its own and writes at most one, never one it reads.
    /// </summary>
    [Flags]
    public enum ScatterCoverUse
    {
        None = 0,
        /// <summary>Its details shrink away where large objects cover the ground (stones, grass).</summary>
        ReadLarge = 1,
        /// <summary>Its details shrink away where small details cover the ground (grass).</summary>
        ReadSmall = 2,
        /// <summary>It covers the ground for the kinds placed after it, as a large object (rocks).</summary>
        WriteLarge = 4,
        /// <summary>It covers the ground for the kinds placed after it, as a small detail (stones).</summary>
        WriteSmall = 8,
    }

    /// <summary>
    /// Ground covered by things placed earlier in the frame (rocks, stones), so that the kinds placed after them (stones,
    /// grass) skip it: a grid around the camera, written and read by ScatterPlacement.hlsl (ScatterCoverWrite, and
    /// ScatterTryCandidate when reading). Kinds that write must be placed before the kinds that read, in the same frame:
    /// rocks in Update, stones in LateUpdate with an earlier execution order, grass after. A kind reading before a
    /// writer this frame doesn't see that writer's cover.
    /// </summary>
    public interface IScatterGpuCover
    {
        /// <summary>Binds the cover of this frame to a kernel. Call it every frame before dispatching.</summary>
        void Bind(ComputeShader shader, int kernel, ScatterCoverUse use);
    }
}
