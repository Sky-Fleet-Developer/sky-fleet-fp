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
        public static void SaveTreeLayer(string path, TreesLayer layer)
        {
            using (FileStream file = File.Open(path, FileMode.OpenOrCreate))
            {
                ExtensionStream extension = new ExtensionStream();
                extension.WriteInt(layer.Trees.Count, file);
                for (int i = 0; i < layer.Trees.Count; i++)
                {
                    extension.WriteInt(layer.Trees[i].Layer, file);
                    extension.WriteInt(layer.Trees[i].NumTree, file);
                    extension.WriteFloat(layer.Trees[i].Pos.x, file);
                    extension.WriteFloat(layer.Trees[i].Pos.y, file);
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
                    pos.Pos = new Vector2(extension.ReadFloat(file), extension.ReadFloat(file));

                    layer.Trees.Add(pos);
                }
            }
        }
    }
}