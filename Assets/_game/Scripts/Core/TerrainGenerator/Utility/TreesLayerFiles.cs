using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.TerrainGenerator;
using Core.ContentSerializer;


namespace Core.TerrainGenerator.Utility
{
    public static class TreesLayerFiles
    {
        public static void SaveTreeLayer(string path, List<TreePos> trees)
        {
            using (FileStream file = File.Open(path, FileMode.OpenOrCreate))
            {
                file.WriteInt(trees.Count);
                for (int i = 0; i < trees.Count; i++)
                {
                    file.WriteInt(trees[i].Layer);
                    file.WriteInt(trees[i].NumTree);
                    file.WriteByte((byte)(trees[i].Rotate * byte.MaxValue));
                    file.WriteFloat(trees[i].Pos.x);
                    file.WriteFloat(trees[i].Pos.y);
                }
            }
        }

        public static void LoadTreeLayer(string path, List<TreePos> layer)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int count = file.ReadInt();
                for(int i = 0; i < count; i++)
                {
                    TreePos pos = new TreePos();
                    pos.Layer = file.ReadInt();
                    pos.NumTree = file.ReadInt();
                    pos.Rotate = (file.ReadByte() / (float)byte.MaxValue);
                    pos.Pos = new Vector2(file.ReadFloat(), file.ReadFloat());
                    layer.Add(pos);
                }
            }
        }
    }
}