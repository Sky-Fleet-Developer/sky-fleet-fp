using UnityEngine;

namespace Runtime.Environment.AirDrag
{
    [CreateAssetMenu(menuName = "SF/Data/AirDrag")]
    public class AirDragSettings : ScriptableObject
    {
        public const int ResultBufferSize = 7;
        public Material material;
        public ComputeShader pixelsToNormalsShader;
        public int resolution = 256;
        [Space(15)] public float turbulenceImpact = 1f;
        [Space(15)] public float normalForceImpact = 1f;
        [Space(15)] [SerializeField] public LayerMask mask;
        [SerializeField] public int layer;
    }
}