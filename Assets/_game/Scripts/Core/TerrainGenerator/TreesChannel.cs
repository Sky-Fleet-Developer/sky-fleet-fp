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
        public TerrainData terrainData;

        public TreesChannel(TerrainData terrainData, string path, Vector2Int chunk, GameObject[] prototypes) : base(chunk, terrainData.size.x)
        {
            this.terrainData = terrainData;
            SetPrototypes(prototypes);
            List<TreePos> zeroLayer = new List<TreePos>();
            deformationLayersCache.Add(zeroLayer);
            TreesLayerFiles.LoadTreeLayer(path, zeroLayer);
            IsReady = true;
        }

        private void SetPrototypes(GameObject[] prototypes)
        {
            TreePrototype[] treePrototypes = new TreePrototype[prototypes.Length];
            for (int i = 0; i < treePrototypes.Length; i++)
            {
                treePrototypes[i] = new TreePrototype();
                treePrototypes[i].prefab = prototypes[i];
            }
            terrainData.treePrototypes = treePrototypes;
        }

        protected override List<TreePos> GetLayerCopy(List<TreePos> source)
        {
            return source.DeepClone();
        }

        protected override void ApplyToCache(TreesMapDeformerModule module)
        {
            module.WriteToTerrainData(terrainData, Position);
        }

        protected override void ApplyToTerrain()
        {
            List<TreeInstance> instances = new List<TreeInstance>();
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
            terrainData.SetTreeInstances(instances.ToArray(), true);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            throw new NotImplementedException();
        // new RectangleAffectSettings(terrainData, Position, terrainData.detailResolution, deformer);
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
