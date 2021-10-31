using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paterns;

using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator
{
    public class TerrainGenerator : Singleton<TerrainGenerator>, ILoadAtStart
    {
        public string directoryLandscapes;

        [System.Serializable]
        public class TerrainsRawsOption
        {
            public int sizeRaw;

            [Space]
            public FileFormatSeeker formatMap;

            [Space]
            public Material materialTerrain;
            [Space]
            public int sideSize;
            public float height;
        }

        [System.Serializable]
        public class TerrainsSplatMapsOption
        {
            public string nameSplat;
        }

        [System.Serializable]
        public class TerrainsLayersMapsOption
        {
            public string nameBaseColor;
            public string nameNormal;
        }

        [System.Serializable]
        public class TerrainsTreesLayersOption
        {
            public string pathToFile;
            public GameObject[] trees;
        }

        [SerializeField] private TerrainsRawsOption terrainOptionRaws = new TerrainsRawsOption();
        [SerializeField] private TerrainsSplatMapsOption terrainsSplatMapsOption = new TerrainsSplatMapsOption();
        [SerializeField] private TerrainsLayersMapsOption terrainsLayersMapsOption = new TerrainsLayersMapsOption();
        [SerializeField] private TerrainsTreesLayersOption terrainsTreesLayersOption = new TerrainsTreesLayersOption();

        Dictionary<Vector2Int, Terrain> terrains;
        List<TerrainData> terrainsDates;
        LayerSettings[] settings;
        List<IDeformer> deformers;

        [Button]
        public Task Load()
        {
            terrains = new Dictionary<Vector2Int, Terrain>();
            terrainsDates = new List<TerrainData>();
            deformers = new List<IDeformer>();


            CorrectDirectory();
            DirectoryInfo directoryMap = GetDirectoryMap();
            LoadTerrains(directoryMap);
            LoadTerrainLayers(directoryMap);
            LoadSplatMaps(directoryMap);
            LoadTreesLayer(directoryMap);

            return Task.CompletedTask;
        }

        private void CorrectDirectory()
        {
            string path = PathStorage.GetPathToLandscapesDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private DirectoryInfo GetDirectoryMap()
        {
            string[] directoryes = Directory.GetDirectories(PathStorage.GetPathToLandscapesDirectory());
            for (int i = 0; i < directoryes.Length; i++)
            {
                DirectoryInfo info = new DirectoryInfo(directoryes[i]);
                if (info.Name == directoryLandscapes)
                {
                    return info;
                }
            }
            return null;
        }
        #region Terrain layers
        private void LoadTerrainLayers(DirectoryInfo directoryMap)
        {
            foreach (KeyValuePair<Vector2Int, Terrain> terrain in terrains)
            {
                FileInfo[] files = GetTerrainLayersPath(directoryMap, terrainsLayersMapsOption.nameBaseColor, terrain.Key);
                FileInfo[] filesNormals = GetTerrainLayersPath(directoryMap, terrainsLayersMapsOption.nameNormal, terrain.Key);
                UnityEngine.TerrainLayer[] layers = new UnityEngine.TerrainLayer[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    Texture2D textureBase = new Texture2D(2, 2);
                    PNGReader.ReadPNG(files[i].FullName, textureBase);
                    
                    layers[i] = new UnityEngine.TerrainLayer();
                    layers[i].name = "Layer " + i;
                    layers[i].metallic = -1;
                    layers[i].smoothness = -1000;
                    layers[i].diffuseTexture = textureBase;

                    if (filesNormals.Length > i)
                    {
                        Texture2D textureNormal = new Texture2D(2, 2);
                        PNGReader.ReadPNG(filesNormals[i].FullName, textureNormal);
                        layers[i].normalMapTexture = textureNormal;
                    }
                }

                terrain.Value.terrainData.terrainLayers = layers;
            }
        }

        private FileInfo[] GetTerrainLayersPath(DirectoryInfo directoryMap, string name, Vector2Int pos)
        {
            return directoryMap.GetFiles(name + "_*-" + pos.x + "_" + pos.y + ".*");
        }
        #endregion
        #region Splat maps
        private void LoadSplatMaps(DirectoryInfo directoryMap)
        {
            Dictionary<Terrain, List<ColorLayer>> colorsLayers = new Dictionary<Terrain, List<ColorLayer>>();

            foreach (KeyValuePair<Vector2Int, Terrain> terrain in terrains)
            {
                FileInfo[] files = GetSlaptMapPath(directoryMap, terrainsSplatMapsOption.nameSplat, terrain.Key);
                if (files.Length != 0)
                {
                    ColorLayer color = new ColorLayer(terrainOptionRaws.sideSize, terrain.Key.x, terrain.Key.y, files[0].FullName);
                    List<ColorLayer> list = null;
                    if (colorsLayers.TryGetValue(terrain.Value, out list))
                    {
                        list.Add(color);
                    }
                    else
                    {
                        colorsLayers.Add(terrain.Value, new List<ColorLayer>(1) { color });
                    }
                }
            }
            SetSplatMap(colorsLayers);
        }

        private void SetSplatMap(Dictionary<Terrain, List<ColorLayer>> colorsLayers)
        {
            Dictionary<Terrain, List<ColorLayer>>.KeyCollection keys = colorsLayers.Keys;
            foreach (Terrain terrain in keys)
            {
                List<ColorLayer> list = null;
                colorsLayers.TryGetValue(terrain, out list);
                int size = list[0].ColorTexture.width;
                terrain.terrainData.alphamapResolution = size;
                int countLayer = terrain.terrainData.terrainLayers.Length;
                float[,,] sets = new float[size, size, countLayer];

                for (int i = 0; i < size; i++)
                {
                    for (int i2 = 0; i2 < size; i2++)
                    {
                        for (int i3 = 0; i3 < countLayer; i3++)
                        {
                            if (i3 % 3 == 0)
                            {
                                sets[i, i2, i3] = list[i3 / 3].ColorTexture.GetPixel(i, i2).r;
                            }
                            else if (i3 % 3 == 1)
                            {
                                sets[i, i2, i3] = list[i3 / 3].ColorTexture.GetPixel(i, i2).g;
                            }
                            else
                            {
                                sets[i, i2, i3] = list[i3 / 3].ColorTexture.GetPixel(i, i2).b;
                            }
                        }
                    }
                }

                terrain.terrainData.SetAlphamaps(0, 0, sets);
            }
        }

        private FileInfo[] GetSlaptMapPath(DirectoryInfo directoryMap, string name, Vector2Int pos)
        {
            return directoryMap.GetFiles(name + "_*-" + pos.x + "_" + pos.y + ".*");
        }
        #endregion
        #region Trees layers
        private void LoadTreesLayer(DirectoryInfo directoryMap)
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
        }
        #endregion

        #region Raws load to terrains;
        private void LoadTerrains(DirectoryInfo directoryMap)
        {
            Dictionary<Vector2Int, string> paths = terrainOptionRaws.formatMap.SearchInFolder(directoryMap.FullName);
            List<HeightMap> maps = new List<HeightMap>();

            foreach (KeyValuePair<Vector2Int, string> path in paths)
            {
                maps.Add(new HeightMap(terrainOptionRaws.sizeRaw, path.Key.x, path.Key.y, path.Value));
            }

            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].ApplyToTerrain(CreateTerrain(maps[i].Pos, maps[i].Pos.x * terrainOptionRaws.sideSize, maps[i].Pos.y * terrainOptionRaws.sideSize, maps[i].SideSize, terrainOptionRaws.sideSize, terrainOptionRaws.height));
            }     

            foreach (KeyValuePair<Vector2Int, Terrain> terrain in terrains)
            {
                Terrain left;
                Terrain right;
                Terrain top;
                Terrain buttom;
                terrains.TryGetValue(new Vector2Int(terrain.Key.x - 1, terrain.Key.y), out left);
                terrains.TryGetValue(new Vector2Int(terrain.Key.x + 1, terrain.Key.y), out right);
                terrains.TryGetValue(new Vector2Int(terrain.Key.x, terrain.Key.y - 1), out top);
                terrains.TryGetValue(new Vector2Int(terrain.Key.x, terrain.Key.y + 1), out buttom);

                terrain.Value.SetNeighbors(left, top, right, buttom);
            }

        }

        private FileInfo[] GetPathToRaws(DirectoryInfo directoryMap)
        {
            return directoryMap.GetFiles("*.r16").ToArray();
        }


        private Terrain CreateTerrain(Vector2Int index, int startPosX, int startPosY, int heightMap, float width, float height)
        {
            GameObject obj = new GameObject("t" + index.x + " " + index.y);

            obj.transform.position = new Vector3(startPosX, 0, -startPosY);

            Terrain ter = obj.AddComponent<Terrain>();
            TerrainData data = new TerrainData();
            ter.terrainData = data;

            ter.drawInstanced = true;
            data.heightmapResolution = heightMap;
            data.size = new Vector3(width, height, width);
            ter.materialTemplate = terrainOptionRaws.materialTerrain;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;

            ter.allowAutoConnect = true;
            terrains.Add(index, ter);
            terrainsDates.Add(ter.terrainData);

            return ter;
        }

        #endregion
        public void RegisterDeformer(IDeformer deformer)
        {
            deformers.Add(deformer);
        }


    }
}