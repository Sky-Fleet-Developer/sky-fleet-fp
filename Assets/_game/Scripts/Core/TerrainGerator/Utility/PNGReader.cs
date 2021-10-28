using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator;

namespace Core.TerrainGenerator.Utility
{
    public static class PNGReader
    {
        static public void ReadPNG(string path, Texture2D texture)
        {
            byte[] buffer = File.ReadAllBytes(path);
            texture.LoadImage(buffer);
        }
    }
}