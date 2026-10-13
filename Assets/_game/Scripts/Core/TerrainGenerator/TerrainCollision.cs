using System;
using System.Collections.Generic;
using Core.Data;
using Core.Misc;
using Core.TerrainGenerator.Settings;
using Core.World;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Core.TerrainGenerator
{
    public class TerrainCollision : MonoBehaviour, IBindMe, ITickable
    {
        [Inject] private CollisionGenerationSettings _settings;
        [Inject] private TerrainProvider.ITerrainProviderHandler _terrainProvider;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;
        [Inject] private TickService _tickService;
        private Dictionary<Vector2Int, Chunk> _activeChunks;
        // Chunks around the player that should have a collider
        private readonly HashSet<Vector2Int> _desired = new();
        // Chunks requested from the GPU and not finished yet
        private readonly HashSet<Vector2Int> _inFlight = new();
        // Set when a chunk had no heightmap data yet, so the next tick retries even without movement
        private bool _retryPending;
        private List<Chunk> _inactiveChunksPool;
        private bool _isBaking;
        private List<BakingItem> _pendingBake = new();
        private List<BakingItem> _bakingQueue = new();
        private List<Vector2Int> _pendingRemove = new();
        private List<Vector2Int> _mask;
        private int _chunkResolution;
        private float _chunkSize;
        private Vector2 _prevPosition;
        private int[] _sourceTriangles;
        private Vector3[] _sourceVertices;
        private ComputeBuffer _transportBuffer;
        private ComputeBuffer _requestBuffer;
        private NativeArray<Vector3> _verticesBuffer;
        private MapGpuWorker _gpuWorker;
        public int TickRate => 30;

        private class Chunk
        {
            public MeshCollider MeshCollider;
            public Mesh Mesh;
            public uint Version;
        }
        
        private class BakingItem
        {
            public Chunk Chunk;
            public Vector2Int Coord;
            // Position of the chunk in the GPU request, i.e. in the readback buffer
            public int ReadbackIndex;

            public BakingItem(Vector2Int coord)
            {
                Chunk = null;
                Coord = coord;
            }
        }

        private void Start()
        {
            _prevPosition = Vector2.positiveInfinity;
            _activeChunks = new ();
            _inactiveChunksPool = new ();
            _mask = new ();
            _chunkResolution = _settings.resolution;
            _chunkSize = _settings.chunkSize;
            if (_chunkSize < 1)
            {
                throw new Exception("Chunk size is too small for terrain collision generation");
            }
            int maxChunksRange = Mathf.CeilToInt(_settings.range / _chunkSize);
            for (int i = -maxChunksRange; i <= maxChunksRange; i++)
            {
                for (int j = -maxChunksRange; j <= maxChunksRange; j++)
                {
                    Vector2 center = new Vector2(i + 0.5f, j + 0.5f) * _chunkSize;
                    if (Vector2.SqrMagnitude(center - Vector2.zero) < _settings.range * _settings.range)
                    {
                        _mask.Add(new Vector2Int(i, j));
                    }
                }
            }

            _terrainData = _terrainProvider.GetHeightmapData();
            _transportBuffer = new ComputeBuffer((_chunkResolution + 1) * (_chunkResolution + 1) * _mask.Count, sizeof(float) * 3, ComputeBufferType.Structured);
            _requestBuffer = new ComputeBuffer(_mask.Count, sizeof(int) * 2);
            _gpuWorker = _terrainProvider.Settings.GpuWorker;
            _verticesBuffer = new NativeArray<Vector3>((_chunkResolution + 1)  * (_chunkResolution + 1) * _mask.Count, Allocator.Persistent);
            
            Bootstrapper.OnLoadComplete.Subscribe(OnLoadComplete);
        }

        private void OnLoadComplete()
        {
            _tickService.Add(this);
        }

        public void Tick()
        {
            Vector3 p = _playerTracker.GetPredictedWorldPosition(5, _chunkSize * .25f);
            Vector2 flatPosition = new Vector2(p.x, p.z);
            if (!_retryPending && Vector2.SqrMagnitude(_prevPosition - flatPosition) < _settings.refreshThreshold * _settings.refreshThreshold)
            {
                return;
            }

            _prevPosition = flatPosition;
            _retryPending = false;

            // The mask is built around the center of chunk (0, 0), so the player's chunk is found with floor
            Vector2Int playerCoord = new Vector2Int(Mathf.FloorToInt(p.x / _chunkSize), Mathf.FloorToInt(p.z / _chunkSize));

            _desired.Clear();
            foreach (var maskItem in _mask)
            {
                _desired.Add(playerCoord + maskItem);
            }

            foreach (var kv in _activeChunks)
            {
                if (!_desired.Contains(kv.Key))
                {
                    _pendingRemove.Add(kv.Key);
                }
            }
            
            foreach (var coord in _pendingRemove)
            {
                var chunk = _activeChunks[coord];
                _inactiveChunksPool.Add(chunk);
                _activeChunks.Remove(coord);
                chunk.MeshCollider.gameObject.SetActive(false);
            }
            _pendingRemove.Clear();

            // Rebuilt every time, so chunks that left the area before being baked are dropped
            _pendingBake.Clear();
            foreach (Vector2Int coord in _desired)
            {
                if (!_activeChunks.ContainsKey(coord) && !_inFlight.Contains(coord))
                {
                    _pendingBake.Add(new BakingItem(coord));
                }
            }

            if (_pendingBake.Count > 0 && !_isBaking)
            {
                BakeMeshesAsync().Forget();
            }
        }
        
        private List<Vector2Int> _temp = new();
        private TerrainData _terrainData;

        private void GetChunksDataFromGpu()
        {
            for (var i = 0; i < _bakingQueue.Count; i++)
            {
                _bakingQueue[i].ReadbackIndex = i;
                _temp.Add(_bakingQueue[i].Coord);
            }
            _requestBuffer.SetData(_temp);
            // Heightmap chunks are sized by heightmapSettings.chunkSizeMeters, not by the render patch size
            _gpuWorker.GetHeightmapForCollisionChunks(_transportBuffer, _requestBuffer, _chunkResolution, _temp.Count, _chunkSize, _terrainData.HeightmapTex, _terrainData.GetMapBuffer(out var mapMin, out var mapSize), mapMin, mapSize,  _terrainProvider.Settings.HeightmapResolution, _terrainProvider.Settings.heightmapSettings.chunkSizeMeters, _terrainProvider.Settings.Height, _terrainProvider.Settings.MaxLoadedChunksByOneSide, _settings.offset, _terrainProvider.Settings.heightmapSettings.sharedEdgePixels);
            _temp.Clear();
        }

        private async UniTask BakeMeshesAsync()
        {
            //Debug.Log($"Start baking {_bakingQueue.Count} meshes");
            _isBaking = true;
            int meshLength = (_chunkResolution + 1) * (_chunkResolution + 1);
            while (_pendingBake.Count > 0)
            {
                // The request and transport buffers hold at most _mask.Count chunks
                int batchSize = Mathf.Min(_pendingBake.Count, _mask.Count);
                _bakingQueue.AddRange(_pendingBake.GetRange(0, batchSize));
                _pendingBake.RemoveRange(0, batchSize);
                foreach (BakingItem item in _bakingQueue)
                {
                    _inFlight.Add(item.Coord);
                }

                GetChunksDataFromGpu();

                await AsyncGPUReadback.RequestIntoNativeArray(ref _verticesBuffer, _transportBuffer);
                if (this == null)
                {
                    return;
                }

                // Drop chunks without heightmap data (retried on the next tick) and chunks no longer needed
                for (var i = _bakingQueue.Count - 1; i >= 0; i--)
                {
                    Vector2Int coord = _bakingQueue[i].Coord;
                    _inFlight.Remove(coord);
                    bool hasData = HasHeightData(_bakingQueue[i].ReadbackIndex * meshLength, meshLength);
                    if (!hasData)
                    {
                        _retryPending = true;
                    }
                    if (!hasData || !_desired.Contains(coord))
                    {
                        _bakingQueue.RemoveAt(i);
                    }
                }

                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    if (_inactiveChunksPool.Count > 0)
                    {
                        _bakingQueue[i].Chunk = _inactiveChunksPool[^1];
                        _inactiveChunksPool.RemoveAt(_inactiveChunksPool.Count - 1);

                        /*mesh.SetIndexBufferData(_sourceTriangles, 0, 0, _sourceTriangles.Length,
                            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                            MeshUpdateFlags.DontValidateIndices); */
                    }
                    else
                    {
                        _bakingQueue[i].Chunk = new Chunk();
                        _bakingQueue[i].Chunk.Mesh = CreateCollisionMesh(_chunkResolution, _chunkSize);
                    }
                    
                    _bakingQueue[i].Chunk.Mesh.SetVertexBufferData(_verticesBuffer, _bakingQueue[i].ReadbackIndex * meshLength, 0, meshLength, 0,
                        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                        MeshUpdateFlags.DontValidateIndices);

                    //new BakeSingleMeshJob(chunk.Mesh.GetInstanceID()).Schedule().Complete();
                }
                
                NativeArray<int> meshIds = new NativeArray<int>(_bakingQueue.Count, Allocator.TempJob);
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    meshIds[i] = _bakingQueue[i].Chunk.Mesh.GetInstanceID();
                }
                
                new BakeJob(meshIds).Schedule(_bakingQueue.Count, 1).Complete();
                meshIds.Dispose();

                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    var chunk = _bakingQueue[i].Chunk;
                    if (!chunk.MeshCollider)
                    {
                        chunk.MeshCollider = new GameObject("CollisionChunk").AddComponent<MeshCollider>();
                        chunk.MeshCollider.transform.SetParent(transform);
                    }
                    else
                    {
                        chunk.MeshCollider.gameObject.SetActive(true);
                    }
                    
                    chunk.MeshCollider.transform.localPosition = new Vector3(_bakingQueue[i].Coord.x * _chunkSize, 0, _bakingQueue[i].Coord.y * _chunkSize);
                    chunk.MeshCollider.sharedMesh = chunk.Mesh;
                    _activeChunks[_bakingQueue[i].Coord] = chunk;
                }
                //Debug.Log($"Baked {count} meshes");
                _bakingQueue.Clear();
            }
            _isBaking = false;
        }

        private bool HasHeightData(int start, int length)
        {
            for (int i = start; i < start + length; i++)
            {
                if (float.IsNaN(_verticesBuffer[i].y))
                {
                    return false;
                }
            }
            return true;
        }

        private struct BakeJob : IJobParallelFor
        {
            private NativeArray<int> _meshIds;

            public BakeJob(NativeArray<int> meshIds)
            {
                _meshIds = meshIds;
            }

            public void Execute(int index)
            {
                Physics.BakeMesh(_meshIds[index], false, MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.CookForFasterSimulation);
            }
        }
        
        public struct BakeSingleMeshJob : IJob
        {
            public int MeshInstanceId;

            public BakeSingleMeshJob(int instanceId)
            {
                MeshInstanceId = instanceId;
            }

            public void Execute()
            {
                Physics.BakeMesh(MeshInstanceId, false);
            }
        }

        private Mesh CreateCollisionMesh(int resolution, float sideSize)
        {
            var mesh = new Mesh();
            int triangleCount = resolution * resolution * 6;
            int verticesPerSide = resolution + 1;
            int vertexCount = verticesPerSide * verticesPerSide;
            
            SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
            subMeshDescriptor.baseVertex = 0;
            subMeshDescriptor.firstVertex = 0;
            subMeshDescriptor.indexCount = triangleCount;
            subMeshDescriptor.indexStart = 0;
            subMeshDescriptor.topology = MeshTopology.Triangles;
            subMeshDescriptor.vertexCount = vertexCount;
            subMeshDescriptor.bounds = new Bounds(Vector3.zero, Vector3.one);
            List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>(1) {subMeshDescriptor};
            
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            };
            mesh.SetVertexBufferParams(vertexCount, layout);
            mesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;

            if (_sourceTriangles == null)
            {
                _sourceTriangles = new int[triangleCount];
                _sourceVertices = new Vector3[vertexCount];
                int tIndex = 0;
                float step = sideSize / (resolution);

                for (int j = 0; j <= resolution; j++)
                {
                    for (int i = 0; i <= resolution; i++)
                    {
                        _sourceVertices[i] = new Vector3(j * step, 0, i * step);
                        int vIndex = i * verticesPerSide + j;
                        if (i < resolution && j < resolution)
                        {
                            int bottomLeft = vIndex;
                            int bottomRight = vIndex + 1;
                            int topLeft = vIndex + verticesPerSide;
                            int topRight = vIndex + verticesPerSide + 1;

                            _sourceTriangles[tIndex++] = bottomLeft;
                            _sourceTriangles[tIndex++] = topLeft;
                            _sourceTriangles[tIndex++] = bottomRight;

                            _sourceTriangles[tIndex++] = bottomRight;
                            _sourceTriangles[tIndex++] = topLeft;
                            _sourceTriangles[tIndex++] = topRight;
                        }
                    }
                }
            }

            mesh.SetVertexBufferData(_sourceVertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            mesh.SetIndexBufferData(_sourceTriangles, 0, 0, triangleCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            Vector3 boundsSize = new Vector3(sideSize, sideSize, sideSize);
            mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);
            return mesh;
        }
        
        private void OnDestroy()
        {
            _tickService?.Remove(this);

            foreach (var mesh in _activeChunks.Values)
            {
                UnityEngine.Object.Destroy(mesh.Mesh);
            }
            foreach (var item in _bakingQueue)
            {
                if (item.Chunk != null && item.Chunk.Mesh)
                {
                    UnityEngine.Object.Destroy(item.Chunk.Mesh);
                }
            }
            foreach (var mesh in _inactiveChunksPool)
            {
                UnityEngine.Object.Destroy(mesh.Mesh);
            }
        }

    }
}