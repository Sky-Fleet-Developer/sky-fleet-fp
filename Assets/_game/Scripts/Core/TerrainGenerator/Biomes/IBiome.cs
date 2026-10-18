using System.Collections.Generic;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Filters;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// A terrain biome: an area of the world around one or several sites (Voronoi cell centers).
    /// Cells are built as a power diagram: a point belongs to the site with the smallest |p - site|² - radius².
    /// </summary>
    public interface IBiome
    {
        /// <summary>Power weight of the biome sites, in meters. A larger radius pushes the cell borders outwards.</summary>
        float Radius { get; }

        /// <summary>Distance from the cell border into the biome, in meters, where the biome influence reaches 100%.</summary>
        float BlendDistance { get; }

        /// <summary>
        /// Area of the biome. Null or <see cref="BiomeShapeType.Cell"/>: the cell of the diagram, see <see cref="Radius"/> and
        /// <see cref="BlendDistance"/>. Otherwise a shape outside of the diagram, which makes the biome a layer.
        /// </summary>
        BiomeShape Shape { get; }

        /// <summary>How the biome heights combine with the terrain.</summary>
        BiomeBlendMode BlendMode { get; }

        /// <summary>Color of the biome in the terrain debug view.</summary>
        Color DebugColor { get; }

        /// <summary>Heights and alphamap of the biome, placed relative to each site and rotated with it. May be null.</summary>
        IBiomeSource Source { get; }

        /// <summary>
        /// Heightmap filters of the biome, in order. They filter the biome's own heights before the biomes are blended,
        /// only on the chunks where the biome has influence; the global filters run after blending. May be null.
        /// </summary>
        IReadOnlyList<IHeightmapFilterSource> Filters { get; }

        /// <summary>Adds the world poses of the biome sites.</summary>
        void GetSites(List<BiomeSitePose> sites);
    }

    public struct BiomeSitePose
    {
        /// <summary>
        /// World position. x and z place the site; y, relative to the terrain, offsets the biome heights there (meters).
        /// </summary>
        public Vector3 Position;
        /// <summary>World rotation around the Y axis, in degrees.</summary>
        public float Yaw;

        public BiomeSitePose(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }
    }
}
