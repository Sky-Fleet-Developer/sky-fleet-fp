using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paterns;
using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;
using Core.SessionManager;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Utility;
using Runtime.Character.Control;
using UnityEditor;

namespace Core.TerrainGenerator
{
    public class TerrainProvider : MonoBehaviour, ILoadAtStart
    {
        public static TerrainProvider Instance;

        public TerrainGenerationSettings settings;

        [System.Serializable]
        public class TerrainsTreesLayersOption
        {
            public string pathToFile;
            public GameObject[] trees;
        }

        [ShowInInspector]
        private Dictionary<Vector2Int, List<TerrainLayer>> layers = new Dictionary<Vector2Int, List<TerrainLayer>>();

        private Dictionary<Vector2Int, Terrain> terrains = new Dictionary<Vector2Int, Terrain>();
        private List<TerrainData> terrainsDates = new List<TerrainData>();
        private List<IDeformer> deformers = new List<IDeformer>();

        public static Terrain GetTerrain(Vector2Int position)
        {
            return Instance.terrains[position];
        }

        [Button]
        public void TestLoad()
        {
            Load(new List<Vector2Int>()
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.one
            });
        }

        public Task Load()
        {
            Instance = this;

            Load(GetCurrentProps());

            return Task.CompletedTask;
        }

        public void Load(IEnumerable<Vector2Int> props)
        {
            if (settings.directory == null) throw new System.Exception("Wrong directory!");

            foreach (Vector2Int prop in props)
            {
                if (!terrains.TryGetValue(prop, out Terrain terrain) || terrain == null) CreateTerrain(prop);

                if (!layers.ContainsKey(prop))
                {
                    layers.Add(prop, new List<TerrainLayer>());
                }

                foreach (LayerSettings layerSettings in settings.settings)
                {
                    layers[prop].Add(layerSettings.MakeTerrainLayer(prop, settings.directory.FullName));
                }
            }

            _ = AwaitForReadyAndApply();
        }

        private async Task AwaitForReadyAndApply()
        {
            foreach (KeyValuePair<Vector2Int, List<TerrainLayer>> layerKV in layers)
            {
                foreach (TerrainLayer terrainLayer in layerKV.Value)
                {
                    while (!terrainLayer.IsReady)
                    {
                        await Task.Delay(100);
                    }
                }
            }


            foreach (KeyValuePair<Vector2Int, List<TerrainLayer>> layer in layers)
            {
                foreach (TerrainLayer terrainLayer in layer.Value)
                {
                    terrainLayer.ApplyToTerrain();
                }
            }
        }

        private IEnumerable<Vector2Int> GetCurrentProps()
        {
            FirstPersonController player = Session.Instance.Player;
            Vector3 playerPosition;
            if (player == null) playerPosition = Vector3.zero;
            else playerPosition = player.transform.position;
            float sI = 1f / settings.propSize;
            Vector2 playerCell =
                new Vector2(playerPosition.x * sI, -playerPosition.z * sI);
            float visibleDistance = settings.visibleDistance * sI;

            Vector2Int playerCellInt = new Vector2Int(Mathf.FloorToInt(playerCell.x), Mathf.FloorToInt(playerCell.y));

            for (int x = playerCellInt.x - 3; x <= playerCellInt.x + 3; x++)
            {
                for (int y = playerCellInt.y - 3; y <= playerCellInt.y + 3; y++)
                {
                    if (Mathf.Abs(playerCell.x - x) < visibleDistance && Mathf.Abs(playerCell.y - y) < visibleDistance)
                        yield return new Vector2Int(x, y);
                }
            }
        }

        #region Trees layers

        /*private void LoadTreesLayer(DirectoryInfo directoryMap)
        {
            TreesLayer layer = new TreesLayer(0, 0, terrainsTreesLayersOption.pathToFile);
            Terrain terrain;
            terrains.TryGetValue(new Vector2Int(0, 0) , out terrain);

            List<TreePrototype> prototypes = new List<TreePrototype>();
            for(int i = 0; i < terrainsTreesLayersOption.trees.Length; i++)
            {
                TreePrototype prototype = new TreePrototype();
                prototype.prefab = terrainsTreesLayersOption.trees[i];
                prototypes.Add(prototype);
            }
            terrain.terrainData.treePrototypes = prototypes.ToArray();
            layer.ApplyToTerrain(terrain);
        }*/

        #endregion


        private Terrain CreateTerrain(Vector2Int prop)
        {
            GameObject obj = new GameObject($"Terrain ({prop.x}, {prop.y})");

            obj.transform.position = new Vector3(prop.x * settings.propSize, 0, -prop.y * settings.propSize);

            Terrain ter = obj.AddComponent<Terrain>();
            TerrainData data = new TerrainData();
            data.name = obj.name;
            ter.terrainData = data;

            ter.drawInstanced = true;
            data.heightmapResolution = settings.heightmapResolution;
            data.size = new Vector3(settings.propSize, settings.height, settings.propSize);
            ter.materialTemplate = settings.material;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;

            ter.allowAutoConnect = true;

            if (terrains.ContainsKey(prop)) terrains[prop] = ter;
            else terrains.Add(prop, ter);

            terrainsDates.Add(ter.terrainData);

            return ter;
        }

        public void RegisterDeformer(IDeformer deformer)
        {
            deformers.Add(deformer);
        }
    }
}