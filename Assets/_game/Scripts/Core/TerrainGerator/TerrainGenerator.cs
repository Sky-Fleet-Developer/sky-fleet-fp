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

namespace Core.TerrainGenerator
{
    public class TerrainGenerator : Singleton<TerrainGenerator>, ILoadAtStart
    {
        [SerializeField] private string directoryLandscapes;
        [SerializeField] private int sizeRaw;

        [Space]
        [SerializeField] private string formatMap;

        [Space]
        [SerializeField] private Material materialTerrain;
        [Space]
        [SerializeField] private int sideSize;
        [SerializeField] private float height;

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
            FileInfo[] paths = GetPathToRaws();
            List<HeightMap> maps = new List<HeightMap>();
            for (int i = 0; i < paths.Length; i++)
            {
                for (int i2 = 0; i2 < paths.Length; i2++)
                {
                    string str = string.Format(formatMap, i, i2);
                    FileInfo find = paths.Where(x => { return x.Name == str; }).FirstOrDefault();
                    if (find != null)
                    {
                        maps.Add(new HeightMap(sizeRaw, i, i2, find.FullName));
                    }
                }
            }
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].ApplyToTerrain(CreateTerrain(maps[i].Pos, maps[i].Pos.x * sideSize, maps[i].Pos.y * sideSize, maps[i].SizeSide, sideSize, 150));
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

                Debug.Log("Left: " + left + " Right: " + right + " Top: " + top + " Buttom: " + buttom, terrain.Value);

                terrain.Value.SetNeighbors(left, top, right, buttom);
            }

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

        private FileInfo[] GetPathToRaws()
        {
            string[] directoryes = Directory.GetDirectories(PathStorage.GetPathToLandscapesDirectory());

            for (int i = 0; i < directoryes.Length; i++)
            {
                DirectoryInfo info = new DirectoryInfo(directoryes[i]);
                if (info.Name == directoryLandscapes)
                {
                    return info.GetFiles("*.r16").ToArray();
                }
            }
            return new FileInfo[0];
        }


        private Terrain CreateTerrain( Vector2Int index, int startPosX, int startPosY, int heightMap, float width, float height)
        {
            GameObject obj = new GameObject("t" + index.x + " " + index.y);

            obj.transform.position = new Vector3(startPosX, 0, -startPosY);

            Terrain ter = obj.AddComponent<Terrain>();
            ter.terrainData = new TerrainData();

            ter.terrainData.heightmapResolution = heightMap;
            ter.terrainData.size = new Vector3(width, height, width);
            ter.materialTemplate = materialTerrain;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;


            terrains.Add(index, ter);
            terrainsDates.Add(ter.terrainData);

            return ter;
        }

        public void RegisterDeformer(IDeformer deformer)
        {
            deformers.Add(deformer);
        }


    }
}