using Core.TerrainGenerator.Surfaces;

namespace Core.Scatter
{
    /// <summary>
    /// What every layer of scattered details has: the surface type it grows on, its density and its density patches.
    /// The layers of one surface type mix, each by its density.
    /// </summary>
    public interface IScatterLayer
    {
        SurfaceType Surface { get; }

        /// <summary>Details per square meter at full density.</summary>
        float Density { get; }

        /// <summary>
        /// Weight of <see cref="Surface"/> in the alphamap, 0..1, below which the layer doesn't appear at all. Above it
        /// the density still follows the weight.
        /// </summary>
        float MinSurfaceWeight { get; }

        /// <summary>Size of the density patches, meters. 0: no patches, the density is even.</summary>
        float NoiseScale { get; }

        /// <summary>Share of the ground covered by the patches.</summary>
        float NoiseCoverage { get; }

        /// <summary>Width of the edges of the patches, 0..1. 0: sharp edges.</summary>
        float NoiseSoftness { get; }

        /// <summary>Layers with the same seed and scale share their patches.</summary>
        int NoiseSeed { get; }

        /// <summary>Grows outside the patches instead.</summary>
        bool NoiseInvert { get; }
    }
}
