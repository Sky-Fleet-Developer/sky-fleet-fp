using System;
using UnityEngine;

namespace Core.TerrainGenerator.Surfaces
{
    /// <summary>
    /// A kind of ground the terrain is painted with. Biome alphamaps tell where each surface type lies;
    /// the terrain material blends the surfaces of a pixel by their height maps.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Surface Type")]
    public class SurfaceType : ScriptableObject
    {
        [SerializeField] private Texture2D albedo;
        [Tooltip("Imported as a normal map")]
        [SerializeField] private Texture2D normal;
        [Tooltip("Height for height-based blending, red channel, 0..1: higher surfaces cover lower ones in transitions")]
        [SerializeField] private Texture2D height;
        [SerializeField, Range(0, 1)] private float metallic;
        [SerializeField, Range(0, 1)] private float roughness = 0.7f;
        [Tooltip("Meters covered by one repetition of the textures")]
        [SerializeField, Min(0.01f)] private float tileSize = 4;

        public Texture2D Albedo => albedo;
        public Texture2D Normal => normal;
        public Texture2D Height => height;
        public float Metallic => metallic;
        public float Roughness => roughness;
        public float TileSize => tileSize;

        /// <summary>Raised when the surface is edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
