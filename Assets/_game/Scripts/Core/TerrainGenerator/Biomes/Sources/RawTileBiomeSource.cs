using System;
using System.Drawing;
using System.IO;
using Core.TerrainGenerator.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes.Sources
{
    /// <summary>
    /// Reads tiles from files: heights from .r16 (16 bit raw), alphamap from .png.
    /// File names are built from the tile coordinates, e.g. "Height_{x}-{y}".
    /// </summary>
    [Serializable]
    public class RawTileBiomeSource : IBiomeSource
    {
        [Tooltip("Folder name inside the landscapes directory")]
        [SerializeField] private string directory;
        [SerializeField] private FileFormatSeeker heightmapFormat = new () { format = "Height_{x}-{y}", extension = "r16" };
        [SerializeField] private FileFormatSeeker alphamapFormat = new () { format = "Splat_{x}-{y}", extension = "png" };
        [SerializeField, Min(1)] private float tileSizeMeters = 7200;
        [Tooltip("Point of the tile grid placed at the biome site, in tiles. (5, 5) puts the corner of tile 5-5 at the site")]
        [SerializeField] private Vector2 origin;
        [Tooltip("Tile files overlap by one pixel: the edge row of a tile equals the edge row of its neighbour (e.g. 257 = 256 + 1)")]
        [SerializeField] private bool sharedEdgePixels = true;

        [NonSerialized] private string _resolvedDirectoryName;
        [NonSerialized] private DirectoryInfo _resolvedDirectory;

        public float TileSizeMeters => tileSizeMeters;
        public Vector2 Origin => origin;
        public bool SharedEdgePixels => sharedEdgePixels;
        public string ContentKey => string.IsNullOrEmpty(directory) || heightmapFormat == null
            ? null
            : $"raw|{directory}|{heightmapFormat.format}.{heightmapFormat.extension}|{alphamapFormat?.format}.{alphamapFormat?.extension}";

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

            target.UploadHeights(heights);
            // Without an alphamap file the tile gets no texture layers rather than the previous tile's ones
            target.UploadAlpha(alpha ?? new Color32[target.Resolution * target.Resolution]);
            return true;
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
            var result = new Color32[resolution * resolution];
            for (int u = 0; u < resolution; u++)
            {
                for (int v = 0; v < resolution; v++)
                {
                    System.Drawing.Color color = bitmap.GetPixel(u, resolution - v - 1);
                    result[u + v * resolution] = new Color32(color.R, color.G, color.B, color.A);
                }
            }
            return result;
        }
    }
}
