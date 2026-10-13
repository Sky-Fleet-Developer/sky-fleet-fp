using System;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>Stores a heightmap filter and reports when its settings are edited.</summary>
    public interface IHeightmapFilterSource
    {
        /// <summary>The stored filter, or null when none is assigned.</summary>
        IHeightmapFilter Filter { get; set; }

        /// <summary>Raised when the filter settings change and the heightmap must be rebaked.</summary>
        event Action Changed;
    }
}
