using System;
using System.Collections.Generic;
using Core.World;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator
{
    [Serializable]
    public class CollisionGenerationSettings
    {
        public float refreshThreshold = 100;
        public float range = 500;
        public PhysicsMaterial physicsMaterial;
        public int layer;
    }
    
    public class Collision : IDisposable
    {
        private TerrainProvider _terrainProvider;
        private CollisionGenerationSettings _settings;
        private Dictionary<SubChunkId, MeshCollider> _activeColliders;
        private Dictionary<SubChunkId, Mesh> _cooking;
        private List<Mesh> _inactiveMeshesPool;
        private bool _isBaking;
        private List<SubChunk> _pendingBake = new();
        private List<SubChunk> _bakingQueue = new();
        private HashSet<SubChunkId> _buffer = new(); 
        
        public Collision(TerrainProvider terrainProvider, CollisionGenerationSettings settings)
        {
            _settings = settings;
            _terrainProvider = terrainProvider;
            _activeColliders = new ();
            _cooking = new ();
            _inactiveMeshesPool = new ();
        }
        
        public void UpdateTrackerPosition(Vector3 position, Vector2Int coord)
        {
            if (Vector3.SqrMagnitude(_prevPosition - position) < _settings.refreshThreshold * _settings.refreshThreshold)
            {
                return;
            }
            foreach (var key in _activeColliders.Keys)
            {
                _buffer.Add(key);
            }
            _prevPosition = position;
            float chunkSize = _terrainProvider.settings.ChunkSize;
            float rangeSqr = _settings.range * _settings.range;
            float chunkComparisonRangeSqr = Mathf.Max(rangeSqr, chunkSize * chunkSize * 1.5f);
            foreach ((Vector2Int channelCoord, HeightChannel channel) in _terrainProvider.EnumerateActiveSurfaceChannels())
            {
                float distSqr = (coord - channelCoord).sqrMagnitude * chunkSize * chunkSize;
                if (distSqr < chunkComparisonRangeSqr)
                {
                    foreach (var subChunk in channel.chunk.GetSubChunks())
                    {
                        Vector3 subChunkCenter = subChunk.SelfWorldCenter;
                        
                        float dSqr = (subChunkCenter - position).sqrMagnitude;
                        Debug.DrawRay(WorldOffset.WorldToSpace(subChunkCenter), Vector3.up * 1000, dSqr < rangeSqr ? Color.green : Color.red, 5);
                        if (dSqr < rangeSqr)
                        {
                            _buffer.Remove(subChunk.Id);
                            EnsureCollider(subChunk);
                        }
                    }
                }
            }
            
            foreach (var subChunkId in _buffer)
            {
                var collider = _activeColliders[subChunkId];
                _inactiveMeshesPool.Add(collider.sharedMesh);
                _activeColliders.Remove(subChunkId);
                collider.sharedMesh = null;
            }
            _buffer.Clear();

            if (_pendingBake.Count > 0 && !_isBaking)
            {
                BakeMeshesAsync().Forget();
            }
        }

        private class VertexDataWrapper
        {
            public NativeArray<SubChunk.PackedVertex> Vertices;

            public VertexDataWrapper(NativeArray<SubChunk.PackedVertex> vertices)
            {
                Vertices = vertices;
            }
        }
        
        private List<VertexDataWrapper> _verticesBufferPool = new(4);
        private Vector3 _prevPosition;
        private int[] _sourceTriangles;

        private async UniTask BakeMeshesAsync()
        {
            //Debug.Log($"Start baking {_bakingQueue.Count} meshes");
            _isBaking = true;
            while (_pendingBake.Count > 0)
            {
                (_bakingQueue, _pendingBake) = (_pendingBake, _bakingQueue);
                while (_verticesBufferPool.Count < _bakingQueue.Count)
                {
                    _verticesBufferPool.Add(new VertexDataWrapper(new NativeArray<SubChunk.PackedVertex>((_bakingQueue[0].Resolution + 1)  * (_bakingQueue[0].Resolution + 1), Allocator.Persistent)));
                }
                int finishedCounter = 0;
                UniTaskCompletionSource tcs = new();
                JobHandle jobHandle = default;
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    int closureI = i;
                    AsyncGPUReadback.RequestIntoNativeArray(ref _verticesBufferPool[i].Vertices, _bakingQueue[i].VertexBuffer, v =>
                    {
                        Mesh mesh;
                        if (_inactiveMeshesPool.Count > 0)
                        {
                            mesh = _inactiveMeshesPool[^1];
                            _inactiveMeshesPool.RemoveAt(_inactiveMeshesPool.Count - 1);
                            
                            mesh.SetVertexBufferData(_verticesBufferPool[closureI].Vertices, 0, 0, _verticesBufferPool[closureI].Vertices.Length, 0,
                                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                                MeshUpdateFlags.DontValidateIndices);

                            /*mesh.SetIndexBufferData(_sourceTriangles, 0, 0, _sourceTriangles.Length,
                                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                                MeshUpdateFlags.DontValidateIndices);*/
                        }
                        else
                        {
                            mesh = CreateCollisionMesh(_bakingQueue[closureI].Id,
                                ref _verticesBufferPool[closureI].Vertices, _bakingQueue[closureI].Resolution,
                                _bakingQueue[closureI].Size);
                        } //Debug.Log($"Add {_bakingQueue[closureI].Id} to cooking, mesh {mesh}");

                        _cooking[_bakingQueue[closureI].Id] = mesh;
                        try
                        {
                            new BakeSingleMeshJob(mesh.GetInstanceID()).Schedule(jobHandle);
                            if (++finishedCounter == _bakingQueue.Count)
                            {
                                tcs.TrySetResult();
                            }
                        }
                        catch (Exception e)
                        {
                            tcs.TrySetException(e);
                        }
                    });
                }
       
                await tcs.Task;
                jobHandle.Complete();
                
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    _cooking.Remove(_bakingQueue[i].Id, out Mesh mesh);
                    if (!mesh)
                    {
                        Debug.LogError($"Failed to bake mesh {_bakingQueue[i].Id}");
                    }

                    var collider = _bakingQueue[i].GetOrCreateColliderComponent();
                    collider.sharedMesh = mesh;
                    _activeColliders[_bakingQueue[i].Id] = collider;
                }
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

        private Mesh CreateCollisionMesh(SubChunkId chunkId, ref NativeArray<SubChunk.PackedVertex> vertices,
            int subChunkResolution, float subChunkSize)
        {
            var mesh = new Mesh {name = chunkId.GetHashCode().ToString()};
            int triangleCount = subChunkResolution * subChunkResolution * 6;
            int verticesPerSide = subChunkResolution + 1;
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
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
            mesh.SetVertexBufferParams(vertexCount, layout);
            mesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;

            if (_sourceTriangles == null)
            {
                _sourceTriangles = new int[triangleCount];

                int tIndex = 0;

                for (int j = 0; j <= subChunkResolution; j++)
                {
                    for (int i = 0; i <= subChunkResolution; i++)
                    {
                        int vIndex = i * verticesPerSide + j;
                        if (i < subChunkResolution && j < subChunkResolution)
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

            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            mesh.SetIndexBufferData(_sourceTriangles, 0, 0, triangleCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            Vector3 boundsSize = new Vector3(subChunkSize, subChunkSize, subChunkSize);
            mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);
            return mesh;
        }
        
        private void EnsureCollider(SubChunk subChunk)
        {
            if(!_activeColliders.ContainsKey(subChunk.Id))
            {
                _pendingBake.Add(subChunk);
            }
        }

        public void Dispose()
        {
            foreach (var mesh in _activeColliders.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            foreach (var mesh in _cooking.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            foreach (var mesh in _inactiveMeshesPool)
            {
                UnityEngine.Object.Destroy(mesh);
            }
        }
    }
}