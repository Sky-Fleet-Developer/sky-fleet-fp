using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Core.World;
using UnityEngine;

namespace Core.TerrainGenerator
{
    public class Chunk : IChunk
    {
        private const int MaxMeshVertices = 10000;

        private bool _isHeightDirty = true;
        private readonly TerrainGenerationSettings _settings;

        private readonly List<SubChunk> _subchunks = new List<SubChunk>();

        //private readonly Dictionary<Subchunk, (Vector2Int min, Vector2Int max)> coverage =
        //    new Dictionary<Subchunk, (Vector2Int min, Vector2Int max)>();
        private readonly int _pieces = 1;
        private readonly Material _material;
        public bool IsChunkVisible { get; set; }
        
        private static List<Material> _pool = new ();

        public static void ClearPool()
        {
            foreach (var material in _pool)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }
            }
            _pool.Clear();
        }

#if UNITY_EDITOR
        static Chunk()
        {
            UnityEditor.EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) ClearPool();
            };
        }
#endif

        public Material Material => _material;

        private Vector2Int _coord;

        public float ChunkSize => _settings.ChunkMeshSize;
        public float Height => _settings.Height;
        public int Resolution => _settings.HeightmapResolution;
        public int ColorMapResolution => _settings.AlphamapResolution;

        public Chunk(string name, Vector2Int coord, Transform parent, TerrainGenerationSettings settings)
        {
            _coord = coord;
            _settings = settings;
            while (!IsPiecesAmountEnough(_pieces, settings.HeightmapResolution))
            {
                _pieces *= 2;
            }

            int pieceResolution = settings.HeightmapResolution / _pieces;
            if (_pool.Count == 0)
            {
                _material = Object.Instantiate(settings.Material);
            }
            else
            {
                _material = _pool[^1];
                _pool.RemoveAt(_pool.Count - 1);
            }
            
            Vector3 position = GetMyWorldPosition();

            var worker = settings.GpuWorker;
            for (int i = 0; i < _pieces * _pieces; i++)
            {
                int x = i / _pieces;
                int y = i % _pieces;
                var invertedLocalCoord = new Vector2Int(y, x); // need to invert local coords for chunks for correct order
                SubChunk subChunk = new SubChunk($"{name}_{i}", parent, position, settings.ChunkMeshSize / _pieces, settings.Height,
                    pieceResolution, settings.HeightmapResolution, _coord, invertedLocalCoord, _pieces, _material, worker);

                Vector2Int min = new Vector2Int(x * pieceResolution, y * pieceResolution);
                Vector2Int max = new Vector2Int(min.x + pieceResolution, min.y + pieceResolution);

                subChunk.SetMinMaxCoverage(min, max);

                _subchunks.Add(subChunk);
                //coverage.Add(subChunk, (min, max));
            }
        }
        
        public IReadOnlyList<SubChunk> GetSubChunks()
        {
            return _subchunks;
        }

        private Vector3 GetMyWorldPosition()
        {
            return new Vector3(_coord.x * ChunkSize, 0, _coord.y * ChunkSize);
        }

        private Vector3 GetMySpacePosition()
        {
            return WorldOffset.WorldToSpace(new Vector3(_coord.x * ChunkSize, 0, _coord.y * ChunkSize));
        }

        private bool IsPiecesAmountEnough(int pieces, int resolution)
        {
            resolution -= 1;
            resolution /= pieces;
            resolution += 1;
            return resolution * resolution * 4 <= MaxMeshVertices;
        }
        
        public void RefreshPosition()
        {
            Vector3 position = GetMyWorldPosition();
            foreach (SubChunk subChunk in _subchunks)
            {
                subChunk.SetChunkPosition(position);
            }
        }

        public void OnChunksRefreshed()
        {
            
        }

        public Transform transform { get; }

        public Vector2Int Coord { get; }

        public void SetHeights(RenderTexture heightmap, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace, int mapSize)
        {
            foreach (SubChunk subchunk in _subchunks)
            {
                subchunk.SetHeights(heightmap, mapBuffer, chunkCoordMapSpace, mapSize);
            }

            _isHeightDirty = true;
        }

        public void Enable()
        {
            
        }

        private bool IsIntersecting(Vector2Int aMin, Vector2Int aMax, int bMinX, int bMinY, int bMaxX, int bMaxY)
        {
            return aMin.x <= bMaxX && aMax.x >= bMinX && aMin.y <= bMaxY && aMax.y >= bMinY;
        }

        public Task PostProcess()
        {
            if (_isHeightDirty)
            {
                foreach (SubChunk subchunk in _subchunks)
                {
                    subchunk.Recalculate();
                }

                _isHeightDirty = false;
            }
            return Task.CompletedTask;
        }

        public void Disable()
        {
            foreach (SubChunk subchunk in _subchunks)
            {
                subchunk.Destroy();
            }

            if (Application.isPlaying)
            {
                //Debug.Log("Hide mat " + GetMyWorldPosition());
                _pool.Add(_material);
            }
            else
            {
                Object.DestroyImmediate(_material);
            }
        }
    }
}