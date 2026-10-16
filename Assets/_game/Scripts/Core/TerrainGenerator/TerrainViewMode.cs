namespace Core.TerrainGenerator
{
    /// <summary>How the landscape material colors the terrain. Values match terrain_view_mode in SampleHeightmap.hlsl.</summary>
    public enum TerrainViewMode
    {
        /// <summary>Normal look of the terrain.</summary>
        Shaded = 0,
        /// <summary>Blended debug colors of the biomes.</summary>
        Biomes = 1,
        /// <summary>Height painted by a gradient.</summary>
        Heightmap = 2,
        /// <summary>Texture array slot used by each chunk, with the chunk uv inside it.</summary>
        Uv = 3,
        /// <summary>Patch size of the quadtree LOD per cell, patch borders outlined.</summary>
        Lod = 4,
    }
}
