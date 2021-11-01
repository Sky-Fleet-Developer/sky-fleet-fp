using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class TreesLayer : TerrainLayer
    {
        public List<TreePos> Trees;
        public TerrainData terrainData;

        public TreesLayer(TerrainData data, string path, Vector2Int position, GameObject[] prototypes) : base(position)
        {
            terrainData = data;
            SetPrototypes(prototypes);            
            Trees = new List<TreePos>();
            TreesLayerFiles.LoadTreeLayer(path, this);
            IsReady = true;
        }

        private void SetPrototypes(GameObject[] prototypes)
        {
            TreePrototype[] treePrototypes = new TreePrototype[prototypes.Length];
            for(int i = 0; i < treePrototypes.Length;i++)
            {
                treePrototypes[i] = new TreePrototype();
                treePrototypes[i].prefab = prototypes[i];
            }
            terrainData.treePrototypes = treePrototypes;
        }

        public TreesLayer( Vector2Int position) : base(position)
        {
            Trees = new List<TreePos>();
        }

        public override void ApplyDeformer(IDeformer deformer)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyToTerrain()
        {
            List<TreeInstance> instances = new List<TreeInstance>();
            for(int i = 0; i < Trees.Count; i++)
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
    }
    
    public struct TreePos
    {
        public int Layer;
        public Vector2 Pos;
        public int NumTree;
        public float Rotate;

        public TreePos(int layer, int numTree, float rotate,Vector2 pos)
        {
            Layer = layer;
            NumTree = numTree;
            Pos = pos;
            Rotate = rotate;
        }
    }
}