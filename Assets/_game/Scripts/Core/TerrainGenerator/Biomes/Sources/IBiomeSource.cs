using Core.TerrainGenerator.Surfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes.Sources
{
    /// <summary>
    /// Source data of a biome: a grid of square tiles in the biome's own space. Tile (x, y) covers
    /// [x, x + 1] × [y, y + 1] tile units; the point <see cref="Origin"/> of that space lies at the biome site,
    /// and the grid rotates with the site. Implementations are plain [Serializable] classes.
    /// </summary>
    public interface IBiomeSource
    {
        /// <summary>Size of one tile in meters.</summary>
        float TileSizeMeters { get; }

        /// <summary>Point of the source space, in tile units, placed at the biome site.</summary>
        Vector2 Origin { get; }

        /// <summary>
        /// True: a tile shares its edge pixels with its neighbours (e.g. 257 = 256 + 1), pixel i lies at i / (resolution - 1).
        /// False: pixel i is a cell centered at (i + 0.5) / resolution.
        /// </summary>
        bool SharedEdgePixels { get; }

        /// <summary>
        /// Identifies the tile content, e.g. the files the tiles are read from. Sources with equal keys share
        /// loaded tiles, so the same data is loaded once. Null when the source has no data at all.
        /// </summary>
        string ContentKey { get; }

        /// <summary>
        /// Writes the heights (normalized, 0..1) and the alphamap of a tile into the target.
        /// The alphamap holds one weight per channel; <see cref="GetSurfaceChannel"/> tells which surface each paints.
        /// Returns false when the source has no data for the tile.
        /// </summary>
        UniTask<bool> LoadTile(Vector2Int tile, BiomeTileTarget target);

        /// <summary>Surface type painted by a channel (0..3: r, g, b, a) of the alphamap.</summary>
        SurfaceChannel GetSurfaceChannel(int channel);
    }
}
