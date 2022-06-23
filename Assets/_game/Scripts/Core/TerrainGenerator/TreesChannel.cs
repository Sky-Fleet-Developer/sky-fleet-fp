using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Settings;
using System.Linq;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class TreesChannel : DeformationChannel
    {
        public List<TreePos> Trees;
        public TerrainData terrainData;

        public TreesChannel(TerrainData terrainData, string path, Vector2Int chunk, GameObject[] prototypes) : base(chunk, terrainData.size.x)
        {
            this.terrainData = terrainData;
            SetPrototypes(prototypes);
            Trees = new List<TreePos>();
            TreesLayerFiles.LoadTreeLayer(path, this);
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

        protected override void ApplyDeformer(IDeformer deformer)
        {
            TreesMapDeformerModule deformerModule = deformer.GetModules<TreesMapDeformerModule>();
            if (deformerModule == null) return;

            deformerModule.WriteToTerrainData(terrainData, Position);
        }

        protected override void ApplyToTerrain()
        {
            List<TreeInstance> instances = new List<TreeInstance>();
            for (int i = 0; i < Trees.Count; i++)
            {
                TreeInstance instance = new TreeInstance();
                instance.widthScale = 1;
                instance.heightScale = 1;
                instance.prototypeIndex = 0;
                instance.rotation = 0;
                instance.rotation = Trees[i].Rotate;
                instance.color = Color.white;
                instance.position = new Vector3(Trees[i].Pos.x, 0, Trees[i].Pos.y);
                instances.Add(instance);
            }
            terrainData.SetTreeInstances(instances.ToArray(), true);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            throw new NotImplementedException();
        // new RectangleAffectSettings(terrainData, Position, terrainData.detailResolution, deformer);
    }

    public struct TreePos
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
    }
}
