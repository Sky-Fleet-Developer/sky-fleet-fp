using System;
using System.Collections.Generic;
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
    
    public class Collision
    {
        private TerrainProvider _terrainProvider;
        private CollisionGenerationSettings _settings;
        private Dictionary<SubChunkId, Mesh> _meshesPool;
        private Dictionary<SubChunkId, Mesh> _coocking;
        private bool _isBaking;

        public Collision(TerrainProvider terrainProvider, CollisionGenerationSettings settings)
        {
            _settings = settings;
            _terrainProvider = terrainProvider;
            _meshesPool = new ();
            _coocking = new ();
        }

        private List<(Chunk, SubChunk)> _bakingQueue = new();
        public void UpdateTrackerPosition(Vector3 position, Vector2Int coord)
        {
            if (Vector3.SqrMagnitude(_prevPosition - position) < _settings.refreshThreshold * _settings.refreshThreshold)
            {
                return;
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
                        Vector3 subChunkCenter = subChunk.SelfCenter;
                        
                        float dSqr = (subChunkCenter - position).sqrMagnitude;
                        if (dSqr < rangeSqr)
                        {
                            TryCreateCollider(subChunk, channel.chunk);
                        }
                    }
                }
            }

            if (_bakingQueue.Count > 0 && !_isBaking)
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

        private async UniTask BakeMeshesAsync()
        {
            Debug.Log($"Start baking {_bakingQueue.Count} meshes");
            _isBaking = true;
            while (_bakingQueue.Count > 0)
            {
                int count = _bakingQueue.Count;
                while (_verticesBufferPool.Count < count)
                {
                    _verticesBufferPool.Add(new VertexDataWrapper(new NativeArray<SubChunk.PackedVertex>((_bakingQueue[0].Item2.Resolution + 1)  * (_bakingQueue[0].Item2.Resolution + 1), Allocator.Persistent)));
                }
                int finishedCounter = 0;
                UniTaskCompletionSource tcs = new();
                JobHandle jobHandle = default;
                for (var i = 0; i < _bakingQueue.Count; i++)
                {
                    int closureI = i;
                    AsyncGPUReadback.RequestIntoNativeArray(ref _verticesBufferPool[i].Vertices, _bakingQueue[i].Item2.VertexBuffer, v =>
                    {
                        var mesh = CreateCollisionMesh(_bakingQueue[closureI].Item2.Id, ref _verticesBufferPool[closureI].Vertices, _bakingQueue[closureI].Item2.Resolution, _bakingQueue[closureI].Item2.Size);
                        _coocking[_bakingQueue[closureI].Item2.Id] = mesh;
                        new BakeSingleMeshJob(mesh.GetInstanceID()).Schedule(jobHandle);
                        if (++finishedCounter == count)
                        {
                            tcs.TrySetResult();
                        }
                    });
                }
       
                await tcs.Task;
                jobHandle.Complete();
                
                int start = _bakingQueue.Count - count;
                for (var i = start; i < _bakingQueue.Count; i++)
                {
                    _coocking.Remove(_bakingQueue[i].Item2.Id, out Mesh mesh);
                    _meshesPool[_bakingQueue[i].Item2.Id] = mesh;
                    _bakingQueue[i].Item2.GetOrCreateColliderComponent().sharedMesh = mesh;
                }
                Debug.Log($"Baked {count} meshes");
                _bakingQueue.RemoveRange(start, count);
                _isBaking = false;
            }
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

            int[] triangles = new int[triangleCount]; 

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

                        triangles[tIndex++] = bottomLeft;
                        triangles[tIndex++] = topLeft;
                        triangles[tIndex++] = bottomRight;

                        triangles[tIndex++] = bottomRight;
                        triangles[tIndex++] = topLeft;
                        triangles[tIndex++] = topRight;
                    }
                }
            }

            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            mesh.SetIndexBufferData(triangles, 0, 0, triangleCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            Vector3 boundsSize = new Vector3(subChunkSize, subChunkSize, subChunkSize);
            mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);
            return mesh;
        }
        
        private void TryCreateCollider(SubChunk subChunk, Chunk chunk)
        {
            MeshCollider collider = subChunk.GetOrCreateColliderComponent();
            if(_meshesPool.TryGetValue(subChunk.Id, out Mesh mesh))
            {
                if (collider.sharedMesh != mesh)
                {
                    Debug.Log($"Update collider {subChunk.Id} - {mesh.name}");
                    collider.sharedMesh = mesh;
                }
            }
            else
            {
                _bakingQueue.Add((chunk, subChunk));
            }
        }
    }
}