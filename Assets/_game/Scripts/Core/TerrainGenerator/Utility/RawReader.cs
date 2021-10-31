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
        public static void ReadRaw16(string path, HeightLayer layer)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int i = 0;
                byte[] buf = new byte[sizeof(ushort)];
                float[] height = new float[layer.SideSize * layer.SideSize];
                while (file.Read(buf, 0, sizeof(ushort)) > 0)
                {
                    float value = (float)BitConverter.ToUInt16(buf, 0) / ushort.MaxValue;
                    height[i] = value;
                    i++;
                }
                for(int j = 0; j < layer.SideSize; j++)
                {
                    for (int k = 0; k < layer.SideSize; k++)
                    {
                        layer.heights[j,k] = height[j * layer.SideSize + k];
                    }
                }
            }
        }
    }
}