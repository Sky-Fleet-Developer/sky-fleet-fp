namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// A GPU pass applied to a heightmap while it is baked. Implementations are plain [Serializable] classes
    /// that hold both the filter settings and the algorithm; they are stored by an <see cref="IHeightmapFilterSource"/>
    /// (an asset for global filters, a component for biome filters), so the same filter type works everywhere.
    /// Each filter reads the previous one's output.
    /// </summary>
    public interface IHeightmapFilter
    {
        /// <summary>Inactive filters are skipped completely.</summary>
        bool IsActive { get; }

        /// <summary>
        /// Writes the filtered heights of every used layer of <see cref="HeightmapFilterContext.Source"/>
        /// into <see cref="HeightmapFilterContext.Destination"/>. Use <see cref="HeightmapFilterContext.Dispatch"/>
        /// with a kernel built on HeightmapFilterCommon.hlsl.
        /// </summary>
        void Apply(in HeightmapFilterContext context);
    }
}
