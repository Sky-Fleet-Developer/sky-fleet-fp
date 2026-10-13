using System;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// A GPU pass applied to the heightmap while it is baked from the raw chunk data.
    /// Filters are discovered among the children of <see cref="TerrainGenerationSettings"/>
    /// and run in the order they are listed there; each one reads the previous one's output.
    /// The baked result is used by both rendering and collision.
    /// </summary>
    public interface IHeightmapFilter
    {
        /// <summary>Inactive filters are skipped completely.</summary>
        bool IsActive { get; }

        /// <summary>Raised when the filter's settings change and the heightmap must be rebaked.</summary>
        event Action Changed;

        /// <summary>
        /// Writes the filtered heights of every used layer of <see cref="HeightmapFilterContext.Source"/>
        /// into <see cref="HeightmapFilterContext.Destination"/>. Use <see cref="HeightmapFilterContext.Dispatch"/>
        /// with a kernel built on HeightmapFilterCommon.hlsl.
        /// </summary>
        void Apply(in HeightmapFilterContext context);
    }
}
