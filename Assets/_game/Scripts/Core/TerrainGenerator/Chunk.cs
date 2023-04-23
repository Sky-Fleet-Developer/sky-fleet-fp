using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    public class Chunk
    {
        private const int MaxMeshVertices = 10000;
        
        private bool isHeightDirty = true;
        private bool isEdgesHeightEdited = true;
        private readonly TerrainGenerationSettings settings;
        private readonly List<Subchunk> subchunks = new List<Subchunk>();
        //private readonly Dictionary<Subchunk, (Vector2Int min, Vector2Int max)> coverage =
        //    new Dictionary<Subchunk, (Vector2Int min, Vector2Int max)>();
        private readonly int pieces;
        private readonly Material material;
        public bool IsChunkVisible;
        
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                foreach (Subchunk subchunk in subchunks)
                {
                    subchunk.Position = position;
                }                
            }
        }

        public Material Material => material;

        private Vector3 position;

        public float ChunkSize => settings.ChunkSize;
        public float Height => settings.Height;
        public int Resolution => settings.HeightmapResolution;
        public int ColorMapResolution => settings.AlphamapResolution;
        
        public Chunk(string name, Transform parent, TerrainGenerationSettings settings)
        {
            this.settings = settings;
            
            pieces = 1;
            while (!IsPiecesAmountEnough(pieces, settings.HeightmapResolution))
            {
                pieces*=2;
            }

            int pieceResolution = settings.HeightmapResolution / pieces;
            material = Object.Instantiate(settings.Material);
            
            for (int i = 0; i < pieces * pieces; i++)
            {
                int x = i / pieces;
                int y = i % pieces;
                Subchunk subchunk = new Subchunk($"{name}_{i}", parent, settings.ChunkSize / pieces, settings.Height,
                    pieceResolution, new Vector2Int(x, y), pieces, material);


                Vector2Int min = new Vector2Int(x * pieceResolution, y * pieceResolution);
                Vector2Int max = new Vector2Int(min.x + pieceResolution, min.y + pieceResolution);
                
                subchunk.SetMinMaxCoverage(min, max);

                subchunks.Add(subchunk);
                //coverage.Add(subchunk, (min, max));
            }
        }
        
        private bool IsPiecesAmountEnough(int pieces, int resolution)
        {
            resolution -= 1;
            resolution /= pieces;
            resolution += 1;
            return resolution * resolution * 4 <= MaxMeshVertices;
        }
        
        public async Task SetHeights(float[,] heights)
        {
            //int xSize = heights.GetLength(0);
            //int ySize = heights.GetLength(1);
            
            foreach (Subchunk subchunk in subchunks)
            {
                //(Vector2Int min, Vector2Int max) = coverage[subchunk];
                //if (IsIntersecting(min, max, startX, startY, xMax, yMax))
                //{
                await subchunk.SetHeights(heights);
                await Task.Yield();
                //}
            }
            
            isEdgesHeightEdited = true;//startX < 2 || startY < 2 || xMax > Resolution - 1 || yMax > Resolution - 1;

            isHeightDirty = true;
            //mesh.vertices = vertices;
        }
        
        private bool IsIntersecting(Vector2Int aMin, Vector2Int aMax, int bMinX, int bMinY, int bMaxX, int bMaxY)
        {
            return aMin.x <= bMaxX && aMax.x >= bMinX && aMin.y <= bMaxY && aMax.y >= bMinY;
        }

        public async Task PostProcess()
        {
            if (isHeightDirty)
            {
                foreach (Subchunk subchunk in subchunks)
                {
                    subchunk.Recalculate();
                }
                isHeightDirty = false;
                if (isEdgesHeightEdited)
                {
                    foreach (Subchunk subchunk in subchunks)
                    {
                        //subchunk.SetNeighbors();
                        await Task.Yield();
                    }

                    isEdgesHeightEdited = false;
                }
            }
        }
        
        public void Destroy()
        {
            foreach (Subchunk subchunk in subchunks)
            {
                subchunk.Destroy();
            }

            if (Application.isPlaying)
            {
                Debug.Log("Hide mat " + position);
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}