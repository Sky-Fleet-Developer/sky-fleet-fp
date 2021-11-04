using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.TerrainGenerator;

namespace Core.TerrainGenerator.Utility
{
    public static class RawReader
    {
        public static void ReadRaw16(string path, float[,] heights)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int i = 0;
                byte[] buf = new byte[sizeof(ushort)];
                float[] height = new float[heights.Length];
                while (file.Read(buf, 0, sizeof(ushort)) > 0)
                {
                    float value = (float)BitConverter.ToUInt16(buf, 0) / ushort.MaxValue;
                    height[i] = value;
                    i++;
                }

                int xL = heights.GetLength(0);
                int yL = heights.GetLength(1);
                for(int x = 0; x < xL; x++)
                {
                    for (int y = 0; y < yL; y++)
                    {
                        heights[yL - 1 - x, y] = height[x * yL + y];
                    }
                }
            }
        }
    }
}