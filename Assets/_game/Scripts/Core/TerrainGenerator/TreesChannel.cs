using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Settings;
using System.Linq;
using Core.Utilities;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class TreesChannel : DeformationChannel<List<TreePos>, TreesMapDeformerModule>
    {
        public Chunk Chunk;

        public TreesChannel(Chunk chunk, float chunkSize, string path, Vector2Int position, GameObject[] prototypes) : base(position, chunkSize)
        {
            this.Chunk = chunk;
            SetPrototypes(prototypes);
            List<TreePos> zeroLayer = new List<TreePos>();
            deformationLayersCache.Add(zeroLayer);
            TreesLayerFiles.LoadTreeLayer(path, zeroLayer);
            loading.SetResult(true);
        }

        private void SetPrototypes(GameObject[] prototypes)
        {
            throw new NotImplementedException();
            /*TreePrototype[] treePrototypes = new TreePrototype[prototypes.Length];
            for (int i = 0; i < treePrototypes.Length; i++)
            {
                treePrototypes[i] = new TreePrototype();
                treePrototypes[i].prefab = prototypes[i];
            }
            Chunk.treePrototypes = treePrototypes;*/
        }

        protected override List<TreePos> GetLayerCopy(List<TreePos> source)
        {
            return source.DeepClone();
        }

        protected override void ApplyToCache(TreesMapDeformerModule module)
        {
            throw new NotImplementedException();

            //module.WriteToTerrainData(Chunk, Position);
        }

        protected override Task ApplyToTerrain()
        {
            throw new NotImplementedException();

            /*List<TreeInstance> instances = new List<TreeInstance>();
            List<TreePos> lastLayer = GetLastLayer();
            for (int i = 0; i < lastLayer.Count; i++)
            {
                TreeInstance instance = new TreeInstance();
                instance.widthScale = 1;
                instance.heightScale = 1;
                instance.prototypeIndex = 0;
                instance.rotation = 0;
                instance.rotation = lastLayer[i].Rotate;
                instance.color = Color.white;
                instance.position = new Vector3(lastLayer[i].Pos.x, 0, lastLayer[i].Pos.y);
                instances.Add(instance);
            }
            Chunk.SetTreeInstances(instances.ToArray(), true);*/
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            throw new NotImplementedException();
        // new RectangleAffectSettings(terrainData, Position, terrainData.detailResolution, deformer);
        
        public override void SetChunk(Chunk chunk)
        {
            IsDirty = true;
            Chunk = chunk;
        }
    }

    public struct TreePos : ICloneable
    {
        public int Layer;
        public Vector2 Pos;
        public int NumTree;
        public float Rotate;

        public TreePos(int layer, int numTree, float rotate, Vector2 pos)
        {
            Layer = layer;
            NumTree = numTree;
            Pos = pos;
            Rotate = rotate;
        }

        public object Clone()
        {
            return new TreePos(Layer, NumTree, Rotate, Pos);
        }
    }
}
