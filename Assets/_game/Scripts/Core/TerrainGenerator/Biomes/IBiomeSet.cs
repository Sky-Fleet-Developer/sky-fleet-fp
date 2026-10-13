using System;
using System.Collections.Generic;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// All biomes of the world, in blending order: later biomes are applied on top of earlier ones.
    /// </summary>
    public interface IBiomeSet
    {
        IReadOnlyList<IBiome> Biomes { get; }

        /// <summary>Raised when a biome was added, removed, reordered, moved or edited.</summary>
        event Action Changed;
    }
}
