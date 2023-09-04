using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class HeightChannel : DeformationChannel<float[,], HeightMapDeformerModule>
    {
        public Chunk chunk { get; private set; }
        public int Resolution { get; }

        public HeightChannel(Chunk chunk, int resolution, float chunkSize, Vector2Int coordinates, string path) : base(coordinates, chunkSize)
        {
            this.chunk = chunk;
            Resolution = resolution;
            ReadTex(path, resolution);
        }

        private async void ReadTex(string path, int resolution)
        {
            float debugTime = Time.realtimeSinceStartup;
            Debug.Log("TIMING: begin read tex " + path);
            deformationLayersCache.Add(path != null ? await RawReader.ReadAsync(path) : new float[resolution + 1, resolution + 1]);
            Debug.Log("TIMING: end read tex " + (Time.realtimeSinceStartup - debugTime));
            loading.SetResult(true);
        }

        protected override void ApplyToCache(HeightMapDeformerModule module)
        {
            module.WriteToChannel(this);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            new RectangleAffectSettings(chunk, Position, chunk.Resolution + 1, deformer);

        protected override Task ApplyToTerrain()
        {
            if(deformationLayersCache[0] == null) return Task.CompletedTask;
            
            return chunk.SetHeights(GetLastLayer());
        }

        public override Task PostApply()
        {
            return chunk.PostProcess();
        }

        protected override float[,] GetLayerCopy(float[,] source)
        {
            return source.Clone() as float[,];
        }
    }
}
