using System.Runtime.InteropServices;
using Core.TerrainGenerator.Utility;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [StructLayout(LayoutKind.Sequential)]
    public class RectangleAffectSettings
    {
        public int resolution;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public float sinRotation;
        public float cosRotation;
        public float fade;

        public const int SizeBytes = 32;

        public RectangleAffectSettings(Terrain terrain, IDeformer deformer)
        {
            resolution = terrain.terrainData.heightmapResolution;
            Rect rect = MathfUtilities.GetAffectRectangle(terrain, deformer.AxisAlignedRect);
            minX = Mathf.CeilToInt(rect.x * resolution);
            minY = Mathf.CeilToInt(rect.y * resolution);
            minX = Mathf.Max(minX, 0);
            minY = Mathf.Max(minY, 0);
            maxX = Mathf.FloorToInt(rect.xMax * resolution);
            maxY = Mathf.FloorToInt(rect.yMax * resolution);
            maxX = Mathf.Min(maxX, resolution);
            maxY = Mathf.Min(maxY, resolution);
            sinRotation = Mathf.Sin(deformer.Rotation);
            cosRotation = Mathf.Cos(deformer.Rotation);
            fade = deformer.Fade;
        }
        public RectangleAffectSettings(Chunk chunk, Vector3 terrainPosition, int resolution, IDeformer deformer)
        {
            this.resolution = resolution;
            Rect rect = MathfUtilities.GetAffectRectangle(chunk, terrainPosition, deformer.AxisAlignedRect);
            minX = Mathf.CeilToInt(rect.x * resolution);
            minY = Mathf.CeilToInt(rect.y * resolution);
            minX = Mathf.Max(minX, 0);
            minY = Mathf.Max(minY, 0);
            maxX = Mathf.FloorToInt(rect.xMax * resolution);
            maxY = Mathf.FloorToInt(rect.yMax * resolution);
            maxX = Mathf.Min(maxX, resolution);
            maxY = Mathf.Min(maxY, resolution);
            sinRotation = Mathf.Sin(deformer.Rotation);
            cosRotation = Mathf.Cos(deformer.Rotation);
            fade = deformer.Fade;
        }
    }
}