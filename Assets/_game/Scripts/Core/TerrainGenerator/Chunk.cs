using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Core.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    public class Chunk
    {
        private const int MaxMeshVertices = 10000;

        private bool _isHeightDirty = true;
        private bool _isEdgesHeightEdited = true;
        private readonly TerrainGenerationSettings _settings;

        private readonly List<Subchunk> _subchunks = new List<Subchunk>();

        //private readonly Dictionary<Subchunk, (Vector2Int min, Vector2Int max)> coverage =
        //    new Dictionary<Subchunk, (Vector2Int min, Vector2Int max)>();
        private readonly int _pieces = 1;
        private readonly Material _material;
        public bool IsChunkVisible;
        
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
            UnityEditor.EditorApplication.playModeStateChanged += (state) => ClearPool();
        }
#endif

        public Material Material => _material;

        private Vector2Int _coord;

        public float ChunkSize => _settings.ChunkSize;
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

            var worker = settings.Settings.OfType<MeshHeightmapChannelSettings>().First().GpuWorker;
            for (int i = 0; i < _pieces * _pieces; i++)
            {
                int x = i / _pieces;
                int y = i % _pieces;
                var invertedLocalCoord = new Vector2Int(y, x); // need to invert local coords for chunks for correct order
                Subchunk subchunk = new Subchunk($"{name}_{i}", parent, position, settings.ChunkSize / _pieces, settings.Height,
                    pieceResolution, settings.HeightmapResolution, invertedLocalCoord, _pieces, _material, worker);

                Vector2Int min = new Vector2Int(x * pieceResolution, y * pieceResolution);
                Vector2Int max = new Vector2Int(min.x + pieceResolution, min.y + pieceResolution);

                subchunk.SetMinMaxCoverage(min, max);

                _subchunks.Add(subchunk);
                //coverage.Add(subchunk, (min, max));
            }
        }

        private Vector3 GetMyWorldPosition()
        {
            return WorldOffset.Offset + new Vector3(_coord.x * ChunkSize, 0, _coord.y * ChunkSize);
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
            foreach (Subchunk subchunk in _subchunks)
            {
                subchunk.SetChunkPosition(position);
            }
        }

        public void SetHeights(ComputeBuffer heights)
        {
            //int xSize = heights.GetLength(0);
            //int ySize = heights.GetLength(1);

            foreach (Subchunk subchunk in _subchunks)
            {
                //(Vector2Int min, Vector2Int max) = coverage[subchunk];
                //if (IsIntersecting(min, max, startX, startY, xMax, yMax))
                //{
                subchunk.SetHeights(heights);
                //}
            }

            /*int kernelHandle = settings.blitArrayToTexShader.FindKernel("BlitR16");
            using (ComputeBuffer buffer = new ComputeBuffer(settings.HeightmapResolution * settings.HeightmapResolution,
                sizeof(float)))
            {
                buffer.SetData(heights);
                settings.blitArrayToTexShader.SetBuffer(kernelHandle, "input", buffer);
                settings.blitArrayToTexShader.SetTexture(kernelHandle, "resultR16", heightmapTexture);
                settings.blitArrayToTexShader.SetInt("resolution", settings.HeightmapResolution);
                settings.blitArrayToTexShader.Dispatch(kernelHandle,
                    Mathf.CeilToInt(settings.HeightmapResolution / 8f + 0.5f),
                    Mathf.CeilToInt(settings.HeightmapResolution / 8f + 0.5f),
                    1);
            }

            RenderTexture.active = heightmapTexture;*/
            
            
            
            _isEdgesHeightEdited = true; //startX < 2 || startY < 2 || xMax > Resolution - 1 || yMax > Resolution - 1;

            _isHeightDirty = true;
            //mesh.vertices = vertices;
        }

        private bool IsIntersecting(Vector2Int aMin, Vector2Int aMax, int bMinX, int bMinY, int bMaxX, int bMaxY)
        {
            return aMin.x <= bMaxX && aMax.x >= bMinX && aMin.y <= bMaxY && aMax.y >= bMinY;
        }

        public async Task PostProcess()
        {
            if (_isHeightDirty)
            {
                foreach (Subchunk subchunk in _subchunks)
                {
                    subchunk.Recalculate();
                }

                _isHeightDirty = false;
                if (_isEdgesHeightEdited)
                {
                    foreach (Subchunk subchunk in _subchunks)
                    {
                        //subchunk.SetNeighbors();
                        await Task.Yield();
                    }

                    _isEdgesHeightEdited = false;
                }
            }
        }

        public void Destroy()
        {
            foreach (Subchunk subchunk in _subchunks)
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