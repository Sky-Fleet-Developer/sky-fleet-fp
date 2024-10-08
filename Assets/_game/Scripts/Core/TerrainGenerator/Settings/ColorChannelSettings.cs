using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public class ColorChannelSettings : ChannelSettings
    {
        [SerializeField, HideInInspector] private int layersCount = 3;
        [SerializeField] private bool normalizeAlphamap;
        [SerializeField] private string layerMaskProperty = "_LayerMask";
        [SerializeField] private ComputeShader blitShader;
        

        [ShowInInspector]
        public int LayersCount
        {
            get => layersCount;
            set
            {
#if UNITY_EDITOR
                if (value <= 0) value = 1;
                
                int newCount = Mathf.CeilToInt(value / 3f);
                int oldCount = splatmapFormats.Count;
                if (newCount < oldCount)
                {
                    splatmapFormats.RemoveAt(newCount);
                }
                else
                {
                    while (newCount > oldCount)
                    {
                        oldCount++;
                        FileFormatSeeker f;
                        if (splatmapFormats.Count > 0) f = new FileFormatSeeker(splatmapFormats[splatmapFormats.Count - 1]);
                        else f = new FileFormatSeeker();
                        splatmapFormats.Add(f);
                    }
                }
#endif
                layersCount = value;
            }
        }

        public List<FileFormatSeeker> splatmapFormats = new List<FileFormatSeeker>(1)
        {
            new FileFormatSeeker()
        };


        public override DeformationChannel MakeDeformationChannel(TerrainProvider terrain, Vector2Int position, string directory)
        {
            List<string> paths = new List<string>(); 
            foreach (FileFormatSeeker format in splatmapFormats)
            {
               string path = format.SearchInFolder(position, directory);
               paths.Add(path);
            }

            Chunk chunk = terrain.GetChunk(position);
            return new ColorChannel(chunk, blitShader, layerMaskProperty, normalizeAlphamap, layersCount, paths, position);
        }
    }
}
