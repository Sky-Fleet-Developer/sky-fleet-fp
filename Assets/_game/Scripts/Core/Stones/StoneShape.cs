using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Stones
{
    /// <summary>
    /// Shape of a kind of small stone, used by <see cref="StoneLayer"/>.
    /// <para>
    /// The mesh is modeled at any scale: it is resized so that its largest dimension is the size the layer picks. The
    /// pivot is where the stone meets the ground, y up. UV0 maps the tiling textures of <see cref="StoneTextureSet"/>
    /// with any scale: the texel density is evened out over all shapes (StoneSettings.TexelDensity). The mesh must
    /// have Read/Write enabled.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Stone Shape")]
    public class StoneShape : ScriptableObject
    {
        [PropertyTooltip("Shape near the camera. Read/Write must be enabled in its import settings")]
        [SerializeField] private Mesh mesh;
        [PropertyTooltip("Simpler shape farther than StoneSettings.DetailedMeshDistance. Empty: the near shape everywhere")]
        [SerializeField] private Mesh farMesh;
        [PropertyTooltip("Largest number of stones drawn with the near shape. The drawn mesh holds a copy of the shape for " +
                         "each of them, so keep it as low as the stones need")]
        [SerializeField, MinValue(16)] private int maxNear = 4_000;
        [PropertyTooltip("Largest number of stones drawn with the far shape")]
        [SerializeField, MinValue(16)] private int maxFar = 16_000;

        public Mesh Mesh => mesh;
        public Mesh FarMesh => farMesh ? farMesh : mesh;
        public int MaxNear => maxNear;
        public int MaxFar => maxFar;

        /// <summary>Raised when the shape is edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
