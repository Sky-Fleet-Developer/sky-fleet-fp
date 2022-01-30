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
                    Vector3 local = GetPosTreeInDeformer(terrain.terrainData, trTrees[i].position);
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

        private Vector3 GetPosTreeInDeformer(TerrainData data, Vector3 posTree)
        {
            posTree.Scale(data.size);
            return Core.transform.InverseTransformPoint(posTree);
        }

        public void WriteToTerrain(Terrain[] terrains)
        {
            foreach (Terrain terrain in terrains)
            {
                WriteToTerrainData(terrain.terrainData, terrain.transform.position);
            }

        }

        public void WriteToTerrainData(TerrainData data, Vector3 pos)
        {
            Rect rectCore = new Rect(Core.LocalRect.x - Core.LocalRect.z / 2, Core.LocalRect.y - Core.LocalRect.w / 2, Core.LocalRect.z, Core.LocalRect.w);
            TreeInstance[] trees = data.treeInstances;
            List<TreeInstance> newTrees = new List<TreeInstance>();
            for (int i = 0; i < trees.Length; i++)
            {
                if (!rectCore.Contains(GetPosTreeInDeformer(data, trees[i].position).XZ()))
                {
                    newTrees.Add(trees[i]);
                }
            }
            data.SetTreeInstances(new TreeInstance[0], true);
            Rect terrainRect = new Rect(0, 0, 1, 1);
            
            foreach (TreeInstance tree in Trees)
            {
                
                Vector3 trPos = tree.position;
                trPos = Core.transform.TransformPoint(trPos);
                trPos = trPos - pos;
                trPos.y = 0;
                trPos *= 1.0f / data.size.x;
                if (terrainRect.Contains(trPos.XZ()))
                {
                    TreeInstance newTree = tree;
                    newTree.position = trPos;
                    newTree.rotation = (Core.transform.rotation * Quaternion.AngleAxis(newTree.rotation, Vector3.up)).eulerAngles.y;
                    newTree.lightmapColor = new Color32();
                    newTree.color = new Color32();
                    newTrees.Add(newTree);
                    Debug.Log("Add : " + newTree.position + " , " + newTree.rotation + " , " + tree.prototypeIndex);
                } 
            }
            data.SetTreeInstances(newTrees.ToArray(), true);
        }
    }
}