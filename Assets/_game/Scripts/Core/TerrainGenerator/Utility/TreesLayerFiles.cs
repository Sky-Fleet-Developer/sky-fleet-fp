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
                ExtensionStream extension = new ExtensionStream();
                extension.WriteInt(trees.Count, file);
                for (int i = 0; i < trees.Count; i++)
                {
                    extension.WriteInt(trees[i].Layer, file);
                    extension.WriteInt(trees[i].NumTree, file);
                    extension.WriteByte((byte)(trees[i].Rotate * byte.MaxValue), file);
                    extension.WriteFloat(trees[i].Pos.x, file);
                    extension.WriteFloat(trees[i].Pos.y, file);
                }
            }
        }

        public static void LoadTreeLayer(string path, TreesLayer layer)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                ExtensionStream extension = new ExtensionStream();
                int count = extension.ReadInt(file);
                for(int i = 0; i < count; i++)
                {
                    TreePos pos = new TreePos();
                    pos.Layer = extension.ReadInt(file);
                    pos.NumTree = extension.ReadInt(file);
                    pos.Rotate = (extension.ReadByte(file) / (float)byte.MaxValue);
                    pos.Pos = new Vector2(extension.ReadFloat(file), extension.ReadFloat(file));
                    layer.Trees.Add(pos);
                }
            }
        }
    }
}