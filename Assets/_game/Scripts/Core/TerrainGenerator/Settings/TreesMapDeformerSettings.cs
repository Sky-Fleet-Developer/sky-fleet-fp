using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class TreesMapDeformerSettings : IDeformerLayerSetting
    {
        public List<TreeInstance> Trees;

        [JsonIgnore]
        public Deformer Core { get; set; }

        public void Init(Deformer core)
        {
            Core = core;
        }

        [Button]
        public void ReadFromTerrain()
        {
            Terrain[] terrains = Core.GetTerrainsContacts();
            Trees = new List<TreeInstance>();

            Rect rectCore = new Rect(Core.LocalRect.x - Core.LocalRect.z / 2, Core.LocalRect.y - Core.LocalRect.w / 2, Core.LocalRect.z, Core.LocalRect.w);
            foreach (Terrain terrain in terrains)
            {
                TreeInstance[] trTrees = terrain.terrainData.treeInstances;
                for (int i = 0; i < trTrees.Length; i++)
                {
                    Vector3 local = GetPosTreeInDeformer(terrain, trTrees[i].position);
                    if (rectCore.Contains(local.XZ()))
                    {
                        TreeInstance tree = trTrees[i];
                        tree.position = local;
                        tree.rotation = (Quaternion.Inverse(Core.transform.rotation) * Quaternion.AngleAxis(tree.rotation, Vector3.up)).eulerAngles.y;
                        Trees.Add(tree);
                    }
                }
            }
        }

        [Button]
        public void WriteToTerrain()
        {
            WriteToTerrain(Core.GetTerrainsContacts());
        }

        private Vector3 GetPosTreeInDeformer(Terrain terrain, Vector3 posTree)
        {
            posTree.Scale(terrain.terrainData.size);
            return Core.transform.InverseTransformPoint(posTree);
        }

        public void WriteToTerrain(Terrain[] terrains)
        {
            Rect rectCore = new Rect(Core.LocalRect.x - Core.LocalRect.z / 2, Core.LocalRect.y - Core.LocalRect.w / 2, Core.LocalRect.z, Core.LocalRect.w);
            foreach (Terrain terrain in terrains)
            {
                TreeInstance[] trees = terrain.terrainData.treeInstances;
                List<TreeInstance> newTrees = new List<TreeInstance>();
                for (int i = 0; i < trees.Length; i++)
                {
                    if (!rectCore.Contains(GetPosTreeInDeformer(terrain, trees[i].position).XZ()))
                    {
                        newTrees.Add(trees[i]);
                    }
                }
                terrain.terrainData.SetTreeInstances(newTrees.ToArray(), true);
            }

            Rect terrainRect = new Rect(0, 0, 1, 1);
            foreach (Terrain terrain in terrains)
            {
                List<TreeInstance> newTrees = new List<TreeInstance>(terrain.terrainData.treeInstances);

                foreach (TreeInstance tree in Trees)
                {
                    Vector3 trPos = tree.position;
                    trPos = Core.transform.TransformPoint(trPos);
                    trPos = terrain.transform.InverseTransformPoint(trPos);
                    trPos *= 1.0f / terrain.terrainData.size.x;
                    if (terrainRect.Contains(trPos.XZ()))
                    {
                        TreeInstance newTree = tree;
                        newTree.position = trPos;
                        newTree.rotation = (Core.transform.rotation * Quaternion.AngleAxis(newTree.rotation, Vector3.up)).eulerAngles.y;
                        newTrees.Add(newTree);
                    }
                }                
                terrain.terrainData.SetTreeInstances(newTrees.ToArray(), true);
            }
        }
    }
}