using System.Collections.Generic;
using System.IO;
using Core.Trees;
using UnityEditor;
using UnityEngine;

namespace GameEditor.Trees
{
    /// <summary>
    /// Bakes a tree prefab into a <see cref="TreeImpostor"/>: a hemisphere of orthographic views in two atlases (see
    /// <see cref="ImpostorRasterizer"/>), made from one level of detail of the prefab (LOD1 by default: it has the
    /// look of the tree without the cost). Colors are those of the materials of that level; the shaders themselves are
    /// not run, so the colors come from <see cref="AlbedoOf"/>. The prefab gets a <see cref="TreeImpostorReference"/>
    /// to the result. Run it again after the prefab changes.
    /// </summary>
    public static class TreeImpostorBaker
    {
        public struct Settings
        {
            public int framesPerSide;
            public int frameSize;
            public int supersample;
            public int padding;
            /// <summary>Level of detail of the prefab's LODGroup to bake.</summary>
            public int lod;

            public static Settings Default => new Settings { framesPerSide = 8, frameSize = 256, supersample = 2, padding = 4, lod = 1 };
        }

        private const string GreyboxFolder = "Assets/_game/Prefabs/Trees/CyanGreybox";
        private const string OutputFolder = GreyboxFolder + "/Impostors";

        [MenuItem("Terrain/Trees/Bake Cyan Tree Impostors")]
        public static void BakeCyanTrees()
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { GreyboxFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                // Trees without levels of detail (ancient ones are rocks) get no impostor
                if (!prefab || !prefab.GetComponent<LODGroup>()) continue;
                Bake(path, Settings.Default);
                count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Baked {count} tree impostors into {OutputFolder}");
        }

        [MenuItem("Terrain/Trees/Bake Impostor Of Selected Prefab")]
        public static void BakeSelected()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
            {
                Debug.LogError("Select a prefab in the Project window");
                return;
            }

            Bake(path, Settings.Default);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Terrain/Trees/Create Tree Impostor Material")]
        public static void CreateMaterial()
        {
            const string shaderPath = "Assets/_game/Visual/Trees/TreeImpostor.shadergraph";
            const string materialPath = "Assets/_game/Visual/Trees/TreeImpostor.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (!shader)
            {
                Debug.LogError($"Shader graph not found: {shaderPath}");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(shader) { name = "TreeImpostor" };
                UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Selection.activeObject = material;
            Debug.Log($"Tree impostor material: {materialPath}. Assign it to the TreeFarField component");
        }

        public static TreeImpostor Bake(string prefabPath, Settings settings)
        {
            var name = Path.GetFileNameWithoutExtension(prefabPath);
            ImpostorMesh mesh;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                mesh = Gather(root, settings.lod);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (mesh.positions.Length == 0)
            {
                Debug.LogError($"{prefabPath}: nothing to bake");
                return null;
            }

            ImpostorRasterizer.Bound(mesh, out var center, out var radius);
            ImpostorRasterizer.Bake(mesh, center, radius, settings.framesPerSide, settings.frameSize, settings.supersample,
                settings.padding, out var albedo, out var normalDepth);

            EnsureFolder();
            var size = settings.framesPerSide * settings.frameSize;
            var albedoTexture = SaveTexture(albedo, size, $"{OutputFolder}/{name}_Albedo.png", true);
            var normalTexture = SaveTexture(normalDepth, size, $"{OutputFolder}/{name}_NormalDepth.png", false);

            var assetPath = $"{OutputFolder}/{name}_Impostor.asset";
            var impostor = AssetDatabase.LoadAssetAtPath<TreeImpostor>(assetPath);
            if (!impostor)
            {
                impostor = ScriptableObject.CreateInstance<TreeImpostor>();
                AssetDatabase.CreateAsset(impostor, assetPath);
            }

            impostor.Set(albedoTexture, normalTexture, settings.framesPerSide, center, radius);
            EditorUtility.SetDirty(impostor);

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // A reference made when the class was still in another file is a missing script
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);
                var reference = contents.GetComponent<TreeImpostorReference>();
                if (!reference) reference = contents.AddComponent<TreeImpostorReference>();
                reference.Impostor = impostor;
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return impostor;
        }

        /// <summary>The meshes of one level of detail (all of the prefab without a LODGroup) in the space of the prefab root.</summary>
        private static ImpostorMesh Gather(GameObject root, int lod)
        {
            var renderers = new List<Renderer>();
            var group = root.GetComponent<LODGroup>();
            if (group)
            {
                var lods = group.GetLODs();
                if (lods.Length > 0) renderers.AddRange(lods[Mathf.Clamp(lod, 0, lods.Length - 1)].renderers);
            }
            else
            {
                renderers.AddRange(root.GetComponentsInChildren<MeshRenderer>());
            }

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            foreach (var renderer in renderers)
            {
                var filter = renderer ? renderer.GetComponent<MeshFilter>() : null;
                if (!filter || !filter.sharedMesh) continue;

                var mesh = filter.sharedMesh;
                var toRoot = root.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var normalMatrix = toRoot.inverse.transpose;
                var meshPositions = mesh.vertices;
                var meshNormals = mesh.normals;
                var meshColors = mesh.colors;
                var meshUvs = mesh.uv;
                var materials = renderer.sharedMaterials;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var material = materials.Length > 0 ? materials[Mathf.Min(sub, materials.Length - 1)] : null;
                    // Vertices are copied per submesh: each takes the color of its own material
                    var map = new Dictionary<int, int>();
                    var indices = mesh.GetTriangles(sub);
                    foreach (var index in indices)
                    {
                        if (!map.TryGetValue(index, out var mapped))
                        {
                            mapped = positions.Count;
                            map.Add(index, mapped);
                            positions.Add(toRoot.MultiplyPoint3x4(meshPositions[index]));
                            normals.Add(normalMatrix.MultiplyVector(meshNormals.Length > index ? meshNormals[index] : Vector3.up).normalized);
                            var vertexColor = meshColors.Length > index ? meshColors[index] : Color.white;
                            var uv = meshUvs.Length > index ? meshUvs[index] : Vector2.zero;
                            colors.Add(AlbedoOf(material, vertexColor, uv));
                        }

                        triangles.Add(mapped);
                    }
                }
            }

            return new ImpostorMesh
            {
                positions = positions.ToArray(), normals = normals.ToArray(), colors = colors.ToArray(),
                triangles = triangles.ToArray(),
            };
        }

        /// <summary>
        /// The color of a vertex: the gradient of the LOD1 shader (tints along the crystal by its vertex color, or along
        /// the petal by its uv) or the base color of the material.
        /// </summary>
        private static Color AlbedoOf(Material material, Color vertexColor, Vector2 uv)
        {
            if (!material) return Color.gray;

            if (material.HasProperty("_TintBase") && material.HasProperty("_TintTip"))
            {
                var along = material.HasProperty("_GradientSource") && material.GetFloat("_GradientSource") > 0.5f ? uv.x : vertexColor.g;
                var color = Color.Lerp(material.GetColor("_TintBase"), material.GetColor("_TintTip"), Mathf.Clamp01(along));
                color.a = 1;
                return color;
            }

            if (material.HasProperty("_BaseColor"))
            {
                var color = material.GetColor("_BaseColor");
                color.a = 1;
                return color;
            }

            return material.HasProperty("_Color") ? material.color : Color.gray;
        }

        private static Texture2D SaveTexture(Color32[] pixels, int size, string path, bool srgb)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter) AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            // Alpha is coverage (albedo) or depth (normals): not transparency to bleed colors into
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder(GreyboxFolder, "Impostors");
        }
    }
}
