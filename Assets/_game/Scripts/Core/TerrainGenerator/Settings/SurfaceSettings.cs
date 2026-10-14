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

        public IReadOnlyList<SurfaceType> Surfaces => surfaces;
        public int TextureResolution => textureResolution;
        public float BlendDepth => blendDepth;
    }
}
