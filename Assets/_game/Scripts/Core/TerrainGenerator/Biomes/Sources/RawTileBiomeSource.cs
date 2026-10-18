using Sirenix.OdinInspector;
using System;
using System.Drawing;
using System.IO;
using Core.TerrainGenerator.Surfaces;
using Core.TerrainGenerator.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes.Sources
{
    /// <summary>
    /// Reads tiles from files: heights from .r16 (16 bit raw), alphamap from .png.
    /// File names are built from the tile coordinates, e.g. "Height_{x}-{y}".
    /// Each channel of the png is the weight of one surface type, see <see cref="GetSurfaceChannel"/>.
    /// </summary>
    [Serializable]
    public class RawTileBiomeSource : IBiomeSource
    {
        [PropertyTooltip("Folder name inside the landscapes directory")]
        [SerializeField] private string directory;
        [SerializeField] private FileFormatSeeker heightmapFormat = new () { format = "Height_{x}-{y}", extension = "r16" };
        [SerializeField] private FileFormatSeeker alphamapFormat = new () { format = "Splat_{x}-{y}", extension = "png" };
        [SerializeField, MinValue(1)] private float tileSizeMeters = 7200;
        [PropertyTooltip("Point of the tile grid placed at the biome site, in tiles. (5, 5) puts the corner of tile 5-5 at the site")]
        [SerializeField] private Vector2 origin;
        [PropertyTooltip("Tile files overlap by one pixel: the edge row of a tile equals the edge row of its neighbour (e.g. 257 = 256 + 1)")]
        [SerializeField] private bool sharedEdgePixels = true;
        [PropertyTooltip("Remap of the heights (normalized, 0..1): the input range of the file becomes the output range. " +
                 "(0, 1) to (0, 1) keeps them. Swapping the output range turns the relief upside down; " +
                 "a narrow output range flattens it, a raised one lifts it")]
        [SerializeField] private Vector2 heightInputRange = new (0, 1);
        [SerializeField] private Vector2 heightOutputRange = new (0, 1);
        [PropertyTooltip("Heights outside the input range take the ends of the output range. Off: they continue the line")]
        [SerializeField] private bool clampHeights = true;
        [PropertyTooltip("Surface type painted by each channel of the alphamap: r, g, b, a. " +
                 "Default: channel i paints surface i of the terrain settings")]
        [SerializeField] private SurfaceChannel[] surfaceChannels = new SurfaceChannel[4];

        [NonSerialized] private string _resolvedDirectoryName;
        [NonSerialized] private DirectoryInfo _resolvedDirectory;

        public float TileSizeMeters => tileSizeMeters;
        public Vector2 Origin => origin;
        public bool SharedEdgePixels => sharedEdgePixels;
        public string ContentKey => string.IsNullOrEmpty(directory) || heightmapFormat == null
            ? null
            : $"raw|{directory}|{heightmapFormat.format}.{heightmapFormat.extension}|{alphamapFormat?.format}.{alphamapFormat?.extension}" +
              (IsHeightRemapped ? $"|remap{heightInputRange}{heightOutputRange}{clampHeights}" : "");

        private bool IsHeightRemapped => heightInputRange != new Vector2(0, 1) || heightOutputRange != new Vector2(0, 1);

        public void Configure(string directory, FileFormatSeeker heightmapFormat, FileFormatSeeker alphamapFormat,
            float tileSizeMeters, Vector2 origin, bool sharedEdgePixels)
        {
            this.directory = directory;
            this.heightmapFormat = heightmapFormat;
            this.alphamapFormat = alphamapFormat;
            this.tileSizeMeters = tileSizeMeters;
            this.origin = origin;
            this.sharedEdgePixels = sharedEdgePixels;
        }

        public SurfaceChannel GetSurfaceChannel(int channel)
        {
            return surfaceChannels != null && channel < surfaceChannels.Length ? surfaceChannels[channel] : default;
        }

        public async UniTask<bool> LoadTile(Vector2Int tile, BiomeTileTarget target)
        {
            DirectoryInfo folder = GetDirectory();
            if (folder == null || heightmapFormat == null)
            {
                return false;
            }

            string heightmapPath = heightmapFormat.SearchInFolder(tile, folder.FullName);
            if (heightmapPath == null)
            {
                return false;
            }

            string alphamapPath = alphamapFormat?.SearchInFolder(tile, folder.FullName);
            UniTask<Color32[]> alphamapLoading = alphamapPath != null
                ? UniTask.RunOnThreadPool(() => ReadPng(alphamapPath))
                : UniTask.FromResult<Color32[]>(null);
            float[,] heights = await RawReader.ReadAsync(heightmapPath);
            Color32[] alpha = await alphamapLoading;
            await UniTask.SwitchToMainThread();
            if (!target.IsValid || heights == null)
            {
                return heights != null;
            }

            RemapHeights(heights);
            target.UploadHeights(heights, sharedEdgePixels);
            // Without an alphamap file the tile gets no texture layers rather than the previous tile's ones
            target.UploadAlpha(alpha ?? new Color32[target.Resolution * target.Resolution], sharedEdgePixels);
            return true;
        }

        // Tiles are remapped as they are loaded and cached remapped, so ContentKey carries the remap
        private void RemapHeights(float[,] heights)
        {
            if (!IsHeightRemapped)
            {
                return;
            }

            float inputWidth = heightInputRange.y - heightInputRange.x;
            if (Mathf.Abs(inputWidth) < 1e-6f)
            {
                inputWidth = 1e-6f;
            }

            for (int x = 0; x < heights.GetLength(0); x++)
            {
                for (int y = 0; y < heights.GetLength(1); y++)
                {
                    float t = (heights[x, y] - heightInputRange.x) / inputWidth;
                    if (clampHeights)
                    {
                        t = Mathf.Clamp01(t);
                    }
                    heights[x, y] = Mathf.Clamp01(Mathf.LerpUnclamped(heightOutputRange.x, heightOutputRange.y, t));
                }
            }
        }

        private DirectoryInfo GetDirectory()
        {
            if (_resolvedDirectoryName != directory)
            {
                _resolvedDirectoryName = directory;
                _resolvedDirectory = string.IsNullOrEmpty(directory) ? null : DirectoryUtilities.GetDirectory(directory);
                if (_resolvedDirectory == null)
                {
                    Debug.LogWarning($"Biome source: directory '{directory}' not found in the landscapes directory");
                }
            }
            return _resolvedDirectory;
        }

        private static Color32[] ReadPng(string path)
        {
            using var bitmap = new Bitmap(path);
            int resolution = bitmap.Width;
            // A png without alpha reads as fully opaque: that's no weight for the alpha channel's surface
            bool hasAlpha = Image.IsAlphaPixelFormat(bitmap.PixelFormat);
            var result = new Color32[resolution * resolution];
            for (int u = 0; u < resolution; u++)
            {
                for (int v = 0; v < resolution; v++)
                {
                    System.Drawing.Color color = bitmap.GetPixel(u, resolution - v - 1);
                    result[u + v * resolution] = new Color32(color.R, color.G, color.B, hasAlpha ? color.A : (byte)0);
                }
            }
            return result;
        }
    }
}
