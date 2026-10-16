using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.Stones
{
    /// <summary>
    /// Tiling textures of the small stones, packed into texture arrays by the Build button (editor only): albedo (BC7),
    /// normal (BC5) and mask (BC7), all at one resolution. The arrays are kept inside this asset.
    /// Mask channels: r metallic, g ambient occlusion, b height (decides the stochastic blend), a smoothness.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Stone Texture Set")]
    public class StoneTextureSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [PropertyTooltip("Color. Empty: middle gray")]
            [SerializeField, PreviewField(48)] private Texture2D albedo;
            [PropertyTooltip("Normal map, imported as a normal map or not. Empty: flat")]
            [SerializeField, PreviewField(48)] private Texture2D normal;
            [PropertyTooltip("r metallic, g ambient occlusion, b height, a smoothness; import it with sRGB off. Empty: not " +
                             "metallic, no occlusion, flat, smoothness 0.3")]
            [SerializeField, PreviewField(48)] private Texture2D mask;

            public Texture2D Albedo => albedo;
            public Texture2D Normal => normal;
            public Texture2D Mask => mask;
        }

        private enum ArrayKind
        {
            Albedo,
            Normal,
            Mask,
        }

        [PropertyTooltip("Pixels along each side of every texture in the arrays")]
        [SerializeField, ValueDropdown(nameof(Resolutions))] private int resolution = 1024;
        [PropertyTooltip("Index in this list = texture index of StoneLayer.Textures")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)] private List<Entry> entries = new ();

        [Title("Built")]
        [SerializeField, ReadOnly] private Texture2DArray albedoArray;
        [SerializeField, ReadOnly] private Texture2DArray normalArray;
        [SerializeField, ReadOnly] private Texture2DArray maskArray;

        private static readonly int[] Resolutions = { 256, 512, 1024, 2048 };

        public Texture2DArray AlbedoArray => albedoArray;
        public Texture2DArray NormalArray => normalArray;
        public Texture2DArray MaskArray => maskArray;

        /// <summary>Textures in the arrays; 0 when they are not built.</summary>
        public int Count => albedoArray && normalArray && maskArray ? albedoArray.depth : 0;

        /// <summary>Pixels along each side of the built textures.</summary>
        public int Resolution => albedoArray ? albedoArray.width : resolution;

        /// <summary>Raised when the set is edited in the inspector or built.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();

#if UNITY_EDITOR
        [Button(ButtonSizes.Large), PropertyOrder(-1)]
        [PropertyTooltip("Packs the textures into the arrays at the resolution above")]
        private void Build()
        {
            if (entries.Count == 0)
            {
                Debug.LogWarning($"Stone texture set {name} has no textures to build", this);
                return;
            }

            try
            {
                ReplaceArray(ref albedoArray, BuildArray(ArrayKind.Albedo), "albedo");
                ReplaceArray(ref normalArray, BuildArray(ArrayKind.Normal), "normal");
                ReplaceArray(ref maskArray, BuildArray(ArrayKind.Mask), "mask");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Changed?.Invoke();
        }

        private void ReplaceArray(ref Texture2DArray current, Texture2DArray built, string kind)
        {
            if (current)
            {
                AssetDatabase.RemoveObjectFromAsset(current);
                DestroyImmediate(current, true);
            }
            built.name = $"{name} {kind}";
            AssetDatabase.AddObjectToAsset(built, this);
            current = built;
        }

        private Texture2DArray BuildArray(ArrayKind kind)
        {
            bool linear = kind != ArrayKind.Albedo;
            TextureFormat format = kind == ArrayKind.Normal ? TextureFormat.BC5 : TextureFormat.BC7;
            var array = new Texture2DArray(resolution, resolution, entries.Count, format, true, linear)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };

            for (int i = 0; i < entries.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Stone textures", $"{kind} {i + 1} / {entries.Count}",
                    ((int)kind + (float)i / entries.Count) / 3f);
                Entry entry = entries[i];
                Texture2D source = kind switch
                {
                    ArrayKind.Albedo => entry?.Albedo,
                    ArrayKind.Normal => entry?.Normal,
                    _ => entry?.Mask,
                };

                Texture2D slice = source ? ReadResized(source, linear) : Solid(DefaultColor(kind), linear);
                if (kind == ArrayKind.Normal && source)
                {
                    RepackNormal(slice);
                }
                slice.Apply(true);
                EditorUtility.CompressTexture(slice, format, TextureCompressionQuality.Best);
                for (int mip = 0; mip < array.mipmapCount; mip++)
                {
                    array.SetPixelData(slice.GetPixelData<byte>(mip), mip, i);
                }
                DestroyImmediate(slice);
            }
            array.Apply(false, false);
            return array;
        }

        private static Color32 DefaultColor(ArrayKind kind) => kind switch
        {
            ArrayKind.Albedo => new Color32(128, 128, 128, 255),
            ArrayKind.Normal => new Color32(128, 128, 0, 255),
            _ => new Color32(0, 255, 128, 77),
        };

        // The source at the array resolution, its texels as stored (no color conversion)
        private Texture2D ReadResized(Texture2D source, bool linear)
        {
            var target = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32,
                linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, target);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var slice = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, linear);
            slice.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return slice;
        }

        private Texture2D Solid(Color32 color, bool linear)
        {
            var slice = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, linear);
            var pixels = new Color32[resolution * resolution];
            Array.Fill(pixels, color);
            slice.SetPixels32(pixels);
            return slice;
        }

        // Normal maps as imported (RGB, RG/BC5 or AG/DXT5nm) to x, y in red and green, for BC5
        private static void RepackNormal(Texture2D slice)
        {
            Color32[] pixels = slice.GetPixels32();
            for (int p = 0; p < pixels.Length; p++)
            {
                Color32 c = pixels[p];
                // RGB and RG: alpha 1, so x is red; AG: red 1, so x is alpha
                byte x = (byte)(c.a * c.r / 255);
                pixels[p] = new Color32(x, c.g, 0, 255);
            }
            slice.SetPixels32(pixels);
        }
#endif
    }
}
