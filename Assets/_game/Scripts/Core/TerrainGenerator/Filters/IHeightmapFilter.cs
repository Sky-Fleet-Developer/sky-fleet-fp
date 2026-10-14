namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// A GPU pass applied to the heightmap, and optionally the alphamap, while the terrain is baked.
    /// Implementations are plain [Serializable] classes
    /// that hold both the filter settings and the algorithm; they are stored by an <see cref="IHeightmapFilterSource"/>
    /// (an asset for global filters, a component for biome filters), so the same filter type works everywhere.
    /// Each filter reads the previous one's output. Every filter can read the alphamap
    /// (<see cref="HeightmapFilterContext.AlphaSource"/>); filters that change surfaces together with heights
    /// also write it (<see cref="WritesAlphamap"/>).
    /// </summary>
    public interface IHeightmapFilter
    {
        /// <summary>Inactive filters are skipped completely.</summary>
        bool IsActive { get; }

        /// <summary>
        /// True when <see cref="Apply"/> writes the alphamap of every used layer into
        /// <see cref="HeightmapFilterContext.AlphaDestination"/>. Otherwise the destination is null and the alphamap
        /// passes through unchanged. Read once per bake.
        /// </summary>
        bool WritesAlphamap { get; }

        /// <summary>
        /// Writes the filtered heights of every used layer of <see cref="HeightmapFilterContext.Source"/>
        /// into <see cref="HeightmapFilterContext.Destination"/>, and the alphamap when <see cref="WritesAlphamap"/>.
        /// Use <see cref="HeightmapFilterContext.Dispatch"/> with a kernel built on HeightmapFilterCommon.hlsl;
        /// kernels using the alphamap include HeightmapFilterAlphamap.hlsl and call
        /// <see cref="HeightmapFilterContext.BindAlphamap"/> first.
        /// </summary>
        void Apply(in HeightmapFilterContext context);
    }
}
