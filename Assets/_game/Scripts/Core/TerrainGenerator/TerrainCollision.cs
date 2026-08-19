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
        private HashSet<Vector2Int> _cooking;
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
        private HeightmapGpuWorker _gpuWorker;
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
            _cooking = new ();
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

            _heightmapData = _terrainProvider.GetHeightmapData();
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
            if (Vector2.SqrMagnitude(_prevPosition - flatPosition) < _settings.refreshThreshold * _settings.refreshThreshold)
            {
                return;
            }
            
            _prevPosition = flatPosition;
            
            Vector2Int playerCoord = new Vector2Int(Mathf.CeilToInt(p.x / _chunkSize), Mathf.CeilToInt(p.z / _chunkSize));

            foreach (Chunk chunk in _activeChunks.Values)
            {
                chunk.Version = 0;
            }
            
            foreach (var maskItem in _mask)
            {
                Vector2Int key = playerCoord + maskItem;
                if (!_activeChunks.TryGetValue(key, out var activeCollider) && !_cooking.Contains(key))
                {
                    _pendingBake.Add(new BakingItem(key));
                }
                else
                {
                    activeCollider.Version++;
                }
            }
            
            foreach (var kv in _activeChunks)
            {
                if (kv.Value.Version == 0)
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

            if (_pendingBake.Count > 0 && !_isBaking)
            {
                BakeMeshesAsync().Forget();
            }
        }
        
        private List<Vector2Int> _temp = new();
        private HeightmapData _heightmapData;

        private void GetChunksDataFromGpu()
        {
            for (var i = 0; i < _bakingQueue.Count; i++)
            {
                _temp.Add(_bakingQueue[i].Coord);
            }
            _requestBuffer.SetData(_temp);
            _gpuWorker.GetHeightmapForCollisionChunks(_transportBuffer, _requestBuffer, _chunkResolution, _temp.Count, _chunkSize, _heightmapData.Texture, _heightmapData.GetMapBuffer(out var mapMin, out var mapSize), mapMin, mapSize,  _terrainProvider.Settings.HeightmapResolution, _terrainProvider.Settings.ChunkSize, _terrainProvider.Settings.Height, _terrainProvider.Settings.MaxLoadedChunksByOneSide, _settings.offset);
            _temp.Clear();
        }

        private async UniTask BakeMeshesAsync()
        {
            //Debug.Log($"Start baking {_bakingQueue.Count} meshes");
            _isBaking = true;
            while (_pendingBake.Count > 0)
            {
                (_bakingQueue, _pendingBake) = (_pendingBake, _bakingQueue);
                
                GetChunksDataFromGpu();
                
                await AsyncGPUReadback.RequestIntoNativeArray(ref _verticesBuffer, _transportBuffer);
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
                    
                    int meshLength = (_chunkResolution + 1) * (_chunkResolution + 1);
                    _bakingQueue[i].Chunk.Mesh.SetVertexBufferData(_verticesBuffer, i * meshLength, 0, meshLength, 0,
                        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                        MeshUpdateFlags.DontValidateIndices);
                    
                    _cooking.Add(_bakingQueue[i].Coord);
                    //new BakeSingleMeshJob(chunk.Mesh.GetInstanceID()).Schedule().Complete();
                }
                
                NativeArray<int> meshIds = new NativeArray<int>(_bakingQueue.Count, Allocator.TempJob);
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    meshIds[i] = _bakingQueue[i].Chunk.Mesh.GetInstanceID();
                }
                
                new BakeJob(meshIds).Schedule(_bakingQueue.Count, 1).Complete();
                
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    var chunk = _bakingQueue[i].Chunk;
                    _cooking.Clear();
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
                _cooking.Clear();
                //Debug.Log($"Baked {count} meshes");
                _bakingQueue.Clear();
            }
            _isBaking = false;
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
                if (item.Chunk.Mesh)
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