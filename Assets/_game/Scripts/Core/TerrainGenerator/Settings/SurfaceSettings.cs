using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [Serializable]
    public class SurfaceSettings
    {
        [Tooltip("Surface types of the terrain. Channel i of a biome alphamap paints surface i, unless the biome source binds another one")]
        [SerializeField] private List<SurfaceType> surfaces = new ();
        [Tooltip("Size of the surface texture arrays; textures of other sizes are resampled")]
        [SerializeField, Min(16)] private int textureResolution = 1024;
        [Tooltip("Height-based blending: height difference (0..1 of the height maps) over which surfaces mix. " +
                 "Smaller is sharper")]
        [SerializeField, Range(0.001f, 1)] private float blendDepth = 0.2f;
        [Tooltip("Hides the tiling of the surface textures: every repetition is offset and rotated at random and " +
                 "neighbouring repetitions are blended. Takes three times the texture samples")]
        [SerializeField] private bool stochasticTiling = true;
        [Tooltip("How much the height maps decide the blend between repetitions: higher parts cover lower ones")]
        [SerializeField, Range(0, 1)] private float stochasticContrast = 0.6f;
        [Tooltip("Largest random rotation of a repetition, in turns. 0 only offsets them")]
        [SerializeField, Range(0, 1)] private float stochasticRotation = 1;

        public IReadOnlyList<SurfaceType> Surfaces => surfaces;
        public int TextureResolution => textureResolution;
        public float BlendDepth => blendDepth;
        public bool StochasticTiling => stochasticTiling;
        public float StochasticContrast => stochasticContrast;
        public float StochasticRotation => stochasticRotation;
    }
}
