using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Texture of the quadtree leaves, one texel per cell: the size in cells of the patch covering the cell, 0 where
    /// there is none. Lets the terrain shader find the LOD of the patch across an edge, to stitch the patches together.
    /// Cells are in terrain space (the space of the patch rects), so the map doesn't change when the world offset moves.
    /// </summary>
    public class PatchLodMap
    {
        private static readonly int LodMapProperty = Shader.PropertyToID("terrain_lod_map");
        private static readonly int LodMapRectProperty = Shader.PropertyToID("terrain_lod_map_rect");

        private Texture2D _texture;
        private float[] _pixels;
        private RectInt _rect;

        public Texture2D Texture => _texture;

        /// <summary>Cells covered by the map.</summary>
        public RectInt Rect => _rect;

        public void Bake(IReadOnlyList<RectInt> leaves)
        {
            if (leaves.Count == 0)
            {
                _rect = new RectInt(0, 0, 1, 1);
            }
            else
            {
                Vector2Int min = leaves[0].min;
                Vector2Int max = leaves[0].max;
                for (int i = 1; i < leaves.Count; i++)
                {
                    min = Vector2Int.Min(min, leaves[i].min);
                    max = Vector2Int.Max(max, leaves[i].max);
                }
                _rect = new RectInt(min, max - min);
            }

            EnsureTexture(_rect.width, _rect.height);
            System.Array.Clear(_pixels, 0, _pixels.Length);
            foreach (RectInt leaf in leaves)
            {
                float size = leaf.width;
                for (int y = leaf.yMin; y < leaf.yMax; y++)
                {
                    int row = (y - _rect.yMin) * _rect.width - _rect.xMin;
                    for (int x = leaf.xMin; x < leaf.xMax; x++)
                    {
                        _pixels[row + x] = size;
                    }
                }
            }

            _texture.SetPixelData(_pixels, 0);
            _texture.Apply(false);
        }

        public void Bind(Material material)
        {
            if (!_texture)
            {
                return;
            }

            material.SetTexture(LodMapProperty, _texture);
            material.SetVector(LodMapRectProperty, new Vector4(_rect.xMin, _rect.yMin, _rect.width, _rect.height));
        }

        private void EnsureTexture(int width, int height)
        {
            if (_texture && _texture.width == width && _texture.height == height)
            {
                return;
            }

            Dispose();
            _texture = new Texture2D(width, height, TextureFormat.RFloat, false, true)
            {
                name = "Terrain patch LOD map",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            _pixels = new float[width * height];
        }

        public void Dispose()
        {
            if (!_texture)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_texture);
            }
            else
            {
                Object.DestroyImmediate(_texture);
            }
            _texture = null;
        }
    }
}
