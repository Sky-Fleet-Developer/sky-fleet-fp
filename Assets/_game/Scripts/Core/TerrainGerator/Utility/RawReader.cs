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
        static public void ReadRaw16(string path, HeightMap map)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int i = 0;
                byte[] buf = new byte[sizeof(ushort)];
                float[] height = new float[map.SideSize * map.SideSize];
                while (file.Read(buf, 0, sizeof(ushort)) > 0)
                {
                    float value = (float)BitConverter.ToUInt16(buf, 0) / (float)ushort.MaxValue;
                    height[i] = value;
                    i++;
                }
                for(int j = 0; j < map.SideSize; j++)
                {
                    for (int k = 0; k < map.SideSize; k++)
                    {
                        map.Heights[j,k] = height[j * map.SideSize + k];
                    }
                }
            }
        }
    }
}