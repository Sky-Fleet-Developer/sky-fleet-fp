using UnityEngine;

namespace Core.TerrainGenerator
{
    public class ErosionSettings : ScriptableObject
    {
        private static readonly int ScaleProperty = Shader.PropertyToID("scale");
        private static readonly int StrengthProperty = Shader.PropertyToID("strength");
        private static readonly int GullyWeightProperty = Shader.PropertyToID("gully_weight");
        private static readonly int DetailProperty = Shader.PropertyToID("detail");
        private static readonly int OnsetProperty = Shader.PropertyToID("onset");
        private static readonly int AssumedSlopeProperty = Shader.PropertyToID("assumed_slope");
        private static readonly int CellScaleProperty = Shader.PropertyToID("cell_scale");
        private static readonly int NormalizationProperty = Shader.PropertyToID("normalization");
        private static readonly int OctavesProperty = Shader.PropertyToID("octaves");
        private static readonly int LacunarityProperty = Shader.PropertyToID("lacunarity");
        private static readonly int GainProperty = Shader.PropertyToID("gain");
        [SerializeField] private float scale = 0.7f;
        [SerializeField] private float strength = 0.22f;
        [SerializeField] private float gullyWeight = 0.5f;
        [SerializeField] private float detail = 1.5f;
        [SerializeField] private Vector4 onset = new Vector4(0.7f, 1.25f, 2.8f, 1.5f);
        [SerializeField] private Vector2 assumedSlope = new Vector2(0.7f, 1.0f);
        [SerializeField] private float cellScale = 0.7f;
        [SerializeField] private float normalization = 0.5f;
        [SerializeField] private float octaves = 5;
        [SerializeField] private float lacunarity = 2.0f;
        [SerializeField] private float gain = 0.5f;

        public bool IsDirty { get; private set; }
        
        public void BindToMaterial(Material material)
        {
            material.SetFloat(ScaleProperty, scale);
            material.SetFloat(StrengthProperty, strength);
            material.SetFloat(GullyWeightProperty, gullyWeight);
            material.SetFloat(DetailProperty, detail);
            material.SetVector(OnsetProperty, onset);
            material.SetVector(AssumedSlopeProperty, assumedSlope);
            material.SetFloat(CellScaleProperty, cellScale);
            material.SetFloat(NormalizationProperty, normalization);
            material.SetFloat(OctavesProperty, octaves);
            material.SetFloat(LacunarityProperty, lacunarity);
            material.SetFloat(GainProperty, gain);
            IsDirty = false;
        }
        
        private void OnValidate() => IsDirty = true;
    }
}