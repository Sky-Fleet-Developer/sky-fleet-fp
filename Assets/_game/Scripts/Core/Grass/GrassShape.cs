using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Grass
{
    /// <summary>
    /// Shape of a kind of grass or small plant, used by <see cref="GrassLayer"/> instead of the built-in blade.
    /// <para>
    /// The mesh is in blade space: root at the origin, y up from 0 (root) to 1 (top) and scaled by the blade height;
    /// x across and z forward, scaled by the blade width; z is the direction the plant leans to. Parts higher up bend
    /// more in the wind and when pushed. The normals light the plant and the vertex colors (rgb) tint it. Submeshes
    /// (material slots of the model) are parts a layer may give their own colors (GrassLayer.Parts); the far mesh
    /// should keep the same order of them. The mesh must have Read/Write enabled.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Grass Shape")]
    public class GrassShape : ScriptableObject
    {
        [PropertyTooltip("Shape near the camera. Read/Write must be enabled in its import settings")]
        [SerializeField] private Mesh mesh;
        [PropertyTooltip("Simpler shape farther than GrassSettings.DetailedMeshDistance. Empty: the near shape everywhere")]
        [SerializeField] private Mesh farMesh;
        [PropertyTooltip("Largest number of plants drawn with the near shape. The drawn mesh holds a copy of the shape for " +
                 "each of them, so keep it as low as the plants need")]
        [SerializeField, MinValue(16)] private int maxNear = 20_000;
        [PropertyTooltip("Largest number of plants drawn with the far shape")]
        [SerializeField, MinValue(16)] private int maxFar = 50_000;

        public Mesh Mesh => mesh;
        public Mesh FarMesh => farMesh ? farMesh : mesh;
        public int MaxNear => maxNear;
        public int MaxFar => maxFar;

        /// <summary>Raised when the shape is edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
