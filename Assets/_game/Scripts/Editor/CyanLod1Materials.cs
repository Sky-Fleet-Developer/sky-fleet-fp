using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace GameEditor.Trees
{
    /// <summary>
    /// Makes the LOD1 materials of the Cyan Tree crystal and petals from Visual/Trees/CyanLod1.shadergraph. Made once:
    /// what is tuned in the Editor stays. Assign them to the LOD1 renderers; the meshes need no change (the shader reads
    /// the vertex colors of the crystal and the uv of the petals).
    /// </summary>
    public static class CyanLod1Materials
    {
        private const string Folder = "Assets/_game/Prefabs/Trees/CyanGreybox";
        private const string ShaderPath = "Assets/_game/Visual/Trees/CyanLod1.shadergraph";

        [MenuItem("Terrain/Trees/Create Cyan LOD1 Materials")]
        public static void Create()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Shader graph not found: {ShaderPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_game/Prefabs/Trees", "CyanGreybox");
            }

            // Crystal: gradient along the crystal from the vertex colors, single sided
            Make(shader, "Crystal", new Color(0.2f, 0.75f, 0.8f), new Color(0.65f, 0.95f, 0.98f), 0, 0.85f, 0.2f, false);
            // Petal: gradient along the petal from the uv, both faces
            Make(shader, "Petal", new Color(0.55f, 0.2f, 0.08f), new Color(0.95f, 0.55f, 0.2f), 1, 0.5f, 0.1f, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cyan LOD1 materials made in {Folder}");
        }

        private static void Make(Shader shader, string name, Color tintBase, Color tintTip, float gradientSource,
            float smoothness, float glow, bool doubleSided)
        {
            var path = $"{Folder}/CyanLod1_{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

            var material = new Material(shader) { name = $"CyanLod1_{name}" };
            material.SetColor("_TintBase", tintBase);
            material.SetColor("_TintTip", tintTip);
            material.SetFloat("_GradientSource", gradientSource);
            material.SetFloat("_SurfaceSmoothness", smoothness);
            material.SetFloat("_Glow", glow);
            if (material.HasProperty("_DoubleSidedEnable")) material.SetFloat("_DoubleSidedEnable", doubleSided ? 1 : 0);
            HDMaterial.ValidateMaterial(material);
            AssetDatabase.CreateAsset(material, path);
        }
    }
}
