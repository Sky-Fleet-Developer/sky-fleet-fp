using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paterns;

using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;

namespace Core.TerrainGenerator
{
    public class TerrainGenerator : Singleton<TerrainGenerator>, ILoadAtStart
    { 
        [SerializeField] private string directoryLandscapes;

        [Space]
        [SerializeField] private string formatMap;

        [Space]
        [SerializeField] private Material materialTerrain;

        Dictionary<Vector2Int, Terrain> terrains;
        List<TerrainData> terrainsDates;
        LayerSettings[] settings;
        List<IDeformer> deformers;


        public Task Load()
        {
            terrains = new Dictionary<Vector2Int, Terrain>();
            terrainsDates = new List<TerrainData>();
            deformers = new List<IDeformer>();

            CorrectDirectory();
            FileInfo[] paths = GetPathToRaws();
            List<HeightMap> maps = new List<HeightMap>();
            for(int i = 0; i < paths.Length; i++)
            {
                for (int i2 = 0; i2 < paths.Length; i2++)
                {
                    string str = string.Format(formatMap, i, i2);
                    FileInfo find = paths.Where(x => { return x.Name == str; }).FirstOrDefault();
                    if (find != null)
                    {
                        maps.Add(new HeightMap(257, i, i2, find.FullName));
                    }
                }
            }
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].ApplyToTerrain(CreateTerrain(maps[i].Pos.x * 1000, maps[i].Pos.y * 1000, maps[i].SizeSide, 1000, 150));
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


        private Terrain CreateTerrain(int startPosX, int startPosY, int heightMap, float width, float height )
        {
            GameObject obj = new GameObject("t" + startPosX + " " + startPosY);

            obj.transform.position = new Vector3(startPosX, 0, -startPosY);

            Terrain ter = obj.AddComponent<Terrain>();
            ter.terrainData = new TerrainData();
            
            ter.terrainData.heightmapResolution = heightMap;
            ter.terrainData.size = new Vector3(width, height, width);
            ter.materialTemplate = materialTerrain;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;

            
            terrains.Add(ter);
            terrainsDates.Add(ter.terrainData);

            return ter;
        }

        public void RegisterDeformer(IDeformer deformer)
        {
            deformers.Add(deformer);
        }


    }
}