using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// A filter that writes surface types into the alphamap. The terrain gives them ids like the surfaces of the
    /// biome sources (<see cref="HeightmapFilterContext.GetSurfaceId"/>). Only the filters of biomes are asked.
    /// </summary>
    public interface IHeightmapFilterSurfaces
    {
        /// <summary>The surface types the filter writes; null entries are ignored.</summary>
        IEnumerable<SurfaceType> Surfaces { get; }
    }
}
