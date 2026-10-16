using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// The terrain as the GPU sees it, for compute shaders that place things on it (grass, details). Binds everything
    /// the shader includes Visual/Map/TerrainHeight.hlsl and TerrainSurfaceWeights.hlsl read: heightmap, alphamap,
    /// chunk map, patch LOD map and geomorphing. Positions there are absolute scene positions or terrain space.
    /// </summary>
    public interface ITerrainGpuData
    {
        /// <summary>The terrain is loaded and can be bound.</summary>
        bool IsReady { get; }

        /// <summary>Surface types by id, the id stored in the alphamap.</summary>
        IReadOnlyList<SurfaceType> Surfaces { get; }

        /// <summary>Raised when the surface ids changed.</summary>
        event Action SurfacesChanged;

        /// <summary>
        /// Raised after the heights were baked again: chunks were loaded or released, or the generation changed. Things
        /// placed once from the heights (not every frame) should place again; heights of unchanged chunks stay the same.
        /// </summary>
        event Action HeightsChanged;

        /// <summary>
        /// Scene position of the origin of terrain space. Terrain space (chunk coordinates, the placement grids) is
        /// the scene minus this; it moves with the world offset. Heights are relative to its y.
        /// </summary>
        Vector3 Origin { get; }

        /// <summary>
        /// Binds the current terrain data to a kernel. Call it every frame before dispatching: the data moves with
        /// the loaded chunks, the LOD and the world offset. The shader must declare
        /// <c>#pragma multi_compile_local _ TERRAIN_GEOMORPH</c>.
        /// </summary>
        void Bind(ComputeShader shader, int kernel);
    }

    /// <summary>
    /// Color the terrain material blends its surfaces towards far from a point, for detail layers that fade out with
    /// distance (grass): past the end of their range the terrain takes their color, so the fade doesn't show.
    /// </summary>
    public interface ITerrainFarTint
    {
        /// <summary>
        /// perSurface: one float4 per surface id (<see cref="ITerrainGpuData.Surfaces"/>): rgb linear color, a amount
        /// 0..1. The tint fades in from range.x to range.y meters away from center (absolute scene position).
        /// </summary>
        void SetFarTint(GraphicsBuffer perSurface, Vector3 center, Vector2 range);

        void ClearFarTint();
    }
}
