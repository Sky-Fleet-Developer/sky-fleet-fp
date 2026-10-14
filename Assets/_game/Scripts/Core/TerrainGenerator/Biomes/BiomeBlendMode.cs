namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// How a biome's heights combine with the terrain. Values match BIOME_BLEND_* in BiomeDiagram.hlsl.
    /// <para>
    /// Normal biomes share the world between them: their heights are mixed by their normalized weights into the base.
    /// Every other mode makes the biome a layer: layers are applied on top of the base in blending order, each one
    /// scaled by its own weight. Normal biomes ignore layers when their weights are computed, so a layer always
    /// has a base under it.
    /// </para>
    /// </summary>
    public enum BiomeBlendMode
    {
        Normal = 0,
        /// <summary>Replaces the heights below.</summary>
        Override = 1,
        /// <summary>Adds the biome heights to the heights below.</summary>
        Add = 2,
        /// <summary>Subtracts the biome heights from the heights below.</summary>
        Subtract = 3,
        /// <summary>Keeps the higher of the biome heights and the heights below.</summary>
        Max = 4,
        /// <summary>Keeps the lower of the biome heights and the heights below.</summary>
        Min = 5,
        /// <summary>Multiplies the heights below by the biome heights (normalized, 1 keeps them).</summary>
        Multiply = 6,
    }
}
