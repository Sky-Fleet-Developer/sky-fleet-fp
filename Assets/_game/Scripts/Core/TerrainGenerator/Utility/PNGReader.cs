using System.IO;
using UnityEngine;

namespace Core.TerrainGenerator.Utility
{
    public static class PNGReader
    {
        public static void ReadPNG(string path, Texture2D texture)
        {
            byte[] buffer = File.ReadAllBytes(path);
            texture.LoadImage(buffer);
        }
    }
}