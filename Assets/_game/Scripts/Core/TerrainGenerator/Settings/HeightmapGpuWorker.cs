using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public class HeightmapGpuWorker
    {
        private static readonly int SourceHeightmap = Shader.PropertyToID("source_heightmap");
        private static readonly int DestinationHeightmap = Shader.PropertyToID("destination_heightmap");
        private static readonly int DeformationRectangleSettings = Shader.PropertyToID("deformation_rectangle_settings");
        private static readonly int DeformationHeightmap = Shader.PropertyToID("deformation_heightmap");
        private static readonly int ChunkSize = Shader.PropertyToID("chunk_size");
        private static readonly int ChunkHeight = Shader.PropertyToID("chunk_height");
        private static readonly int Vertices = Shader.PropertyToID("vertices");
        private static readonly int VerticesCount = Shader.PropertyToID("vertices_count");
        private static readonly int VerticesWidthCount = Shader.PropertyToID("vertices_width_count");
        private static readonly int NormalDx = Shader.PropertyToID("normal_dx");
        private static readonly int NormalDt = Shader.PropertyToID("normal_dt");
        private static readonly int HeightmapStartX = Shader.PropertyToID("heightmap_start_x");
        private static readonly int HeightmapStartY = Shader.PropertyToID("heightmap_start_y");
        private static readonly int MapSize = Shader.PropertyToID("map_size");
        private static readonly int ChunkCoordX = Shader.PropertyToID("chunk_coord_x");
        private static readonly int ChunkCoordY = Shader.PropertyToID("chunk_coord_y");
        private static readonly int Map = Shader.PropertyToID("map");
        private static readonly int ChunkHeightmap = Shader.PropertyToID("chunk_heightmap");
        private static readonly int DestinationCollisionMap = Shader.PropertyToID("destination_collision_map");
        private static readonly int CollisionMapRequest = Shader.PropertyToID("collision_map_request");
        private static readonly int CollisionChunksCount = Shader.PropertyToID("collision_chunks_count");
        private static readonly int HeightmapChunkResolution = Shader.PropertyToID("heightmap_chunk_resolution");
        private static readonly int CollisionChunkResolution = Shader.PropertyToID("collision_chunk_resolution");
        private static readonly int CollisionToHeightmapMul = Shader.PropertyToID("collision_to_heightmap_mul");
        private static readonly int CollisionChunkSize = Shader.PropertyToID("collision_chunk_size");
        private static readonly int HeightmapPixelSize = Shader.PropertyToID("heightmap_pixel_size");
        private static readonly int CollisionPixelSize = Shader.PropertyToID("collision_pixel_size");
        private static readonly int FlipXYWhenImportRawHeightmap = Shader.PropertyToID("flip_x_y_when_import_raw_heightmap");
        private static readonly int SlotsCountInv = Shader.PropertyToID("slots_count_inv");
        private static readonly int HeightmapPixelSizeUVSpace = Shader.PropertyToID("heightmap_pixel_size_uv_space");
        private static readonly int CollisionOffset = Shader.PropertyToID("collision_offset");
        private readonly int _copyHeightmapKernel;
        private readonly int _applyDeformationKernel;
        private readonly int _alignVerticesToHeightmapKernel;
        private readonly int _insertRawDataToTexKernel;
        private readonly int _getCollisionMapKernel;
        private readonly int _testAlignSineKernel;
        
        
        private ComputeShader _shader;
        private Dictionary<int, ComputeBuffer> _activeRectSettingsBuffers = new ();
        private int _rectSettingsBufferIndex = 0;
        private List<ComputeBuffer> _rectSettingsBuffersPool = new ();

        public HeightmapGpuWorker(ComputeShader shader)
        {
            _shader = shader;
            //_copyHeightmapKernel = _shader.FindKernel("CSCopyHeightmap");
            //_applyDeformationKernel = _shader.FindKernel("CSApplyDeformation");
            _alignVerticesToHeightmapKernel = _shader.FindKernel("CSAlignVerticesToHeightmap");
            _insertRawDataToTexKernel = _shader.FindKernel("CSInsertRawDataToTex");
            _getCollisionMapKernel = _shader.FindKernel("CSGetCollisionMap");
            //_testAlignSineKernel = _shader.FindKernel("CSTestAlignSine");
        }


        public ComputeBuffer CopyHeightBuffer(ComputeBuffer source)
        {
            ComputeBuffer copy = new ComputeBuffer(source.count, sizeof(float));
            _shader.SetBuffer(_copyHeightmapKernel, SourceHeightmap, source);
            _shader.SetBuffer(_copyHeightmapKernel, DestinationHeightmap, copy);
            int treadGroups = source.count / 64 + (source.count % 64 > 0 ? 1 : 0);
            _shader.Dispatch(_copyHeightmapKernel, treadGroups, 1, 1);
            return copy;
        }

        public int BindRectSettings(RectangleAffectSettings settings)
        {
            ComputeBuffer buffer;
            if (_rectSettingsBuffersPool.Count < 0)
            {
                buffer = new ComputeBuffer(1, RectangleAffectSettings.SizeBytes);
            }
            else
            {
                buffer = _rectSettingsBuffersPool[^1];
                _rectSettingsBuffersPool.RemoveAt(_rectSettingsBuffersPool.Count - 1);
            }
            _activeRectSettingsBuffers.Add(_rectSettingsBufferIndex++, buffer);
            return _rectSettingsBufferIndex - 1;
        }

        public void UnbindRectSettings(int index)
        {
            _rectSettingsBuffersPool.Add(_activeRectSettingsBuffers[index]);
            _activeRectSettingsBuffers.Remove(index);
        }

        public void ApplyDeformation(int settingsBindId, ComputeBuffer dataBuffer, ComputeBuffer source, IEnumerable<ComputeBuffer> destinations, float chunkSize, float chunkHeight, int resolution)
        {
            _shader.SetBuffer(_applyDeformationKernel, SourceHeightmap, source);
            _shader.SetBuffer(_applyDeformationKernel, DeformationHeightmap, dataBuffer);
            _shader.SetBuffer(_applyDeformationKernel, DeformationRectangleSettings, _activeRectSettingsBuffers[settingsBindId]);
            _shader.SetFloat(ChunkSize, chunkSize);
            _shader.SetFloat(ChunkHeight, chunkHeight);
            int treadGroups = resolution / 8 + (resolution % 8 > 0 ? 1 : 0);

            foreach (ComputeBuffer destination in destinations)
            {
                _shader.SetBuffer(_applyDeformationKernel, DestinationHeightmap, destination);
                _shader.Dispatch(_applyDeformationKernel, treadGroups, treadGroups, 1);
            }
        }

        //public void AlignVerticesToHeightmap(GraphicsBuffer vertexBuffer, ComputeBuffer heightmap, int resolution, float chunkSize, float chunkHeight)
        //{
        //    AlignVerticesToHeightmap(vertexBuffer, heightmap, resolution, resolution, chunkSize, chunkHeight, Vector2Int.zero, new Vector2Int(resolution, resolution));
        //}
        
        public void AlignVerticesToHeightmap(GraphicsBuffer vertexBuffer, RenderTexture heightmap, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace, int mapSize, int meshResolution, int heightmapResolution, float chunkSize, float chunkHeight, Vector2Int minCoverage)
        {
            lock (_shader)
            {
                //Debug.LogError($"AlignVerticesToHeightmap: vertices = {vertexBuffer.count}, {meshResolution}, {chunkSize}, {chunkHeight}");
                int treadGroups = vertexBuffer.count / 8 + (vertexBuffer.count % 8 > 0 ? 1 : 0);
                
                BindVertexBuffer(_alignVerticesToHeightmapKernel, vertexBuffer, meshResolution, chunkSize, chunkHeight);
                BindHeightmapAsSource(_alignVerticesToHeightmapKernel, heightmap, heightmapResolution, minCoverage);
                BindMap(_alignVerticesToHeightmapKernel, mapBuffer, chunkCoordMapSpace, mapSize);

                _shader.Dispatch(_alignVerticesToHeightmapKernel, treadGroups, treadGroups, 1);
            }
        }

        private void BindVertexBuffer(int kernelIndex, GraphicsBuffer vertexBuffer, int meshResolution, float chunkSize, float chunkHeight)
        {
            _shader.SetBuffer(kernelIndex, Vertices, vertexBuffer);
            _shader.SetInt(VerticesCount, vertexBuffer.count);
            _shader.SetInt(VerticesWidthCount, meshResolution);
            _shader.SetFloat(NormalDx, chunkSize / meshResolution * 2);
            _shader.SetFloat(NormalDt, meshResolution / chunkSize * 0.5f);
            _shader.SetFloat(ChunkSize, chunkSize);
            _shader.SetFloat(ChunkHeight, chunkHeight);
        }

        public void TestAlignSine(GraphicsBuffer vertexBuffer, int resolution, float chunkSize)
        {
            int treadGroups = vertexBuffer.count / 64 + (vertexBuffer.count % 64 > 0 ? 1 : 0);
            _shader.SetBuffer(_testAlignSineKernel, Vertices, vertexBuffer);
            _shader.SetInt(VerticesCount, vertexBuffer.count);
            _shader.SetInt(VerticesWidthCount, resolution + 1);
            _shader.SetFloat(ChunkSize, chunkSize);
            _shader.SetFloat(NormalDx, chunkSize / (resolution - 1));
            _shader.SetFloat(NormalDt, (resolution - 1) / chunkSize);

            _shader.Dispatch(_testAlignSineKernel, treadGroups, 1, 1);
        }

        public void InsertDataToBuffer(ComputeBuffer chunkSourceData, RenderTexture heightmap, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace, int mapSize, int heightmapChunkResolution)
        {
            lock (_shader)
            {
                int treadGroups = heightmapChunkResolution / 8 + (heightmapChunkResolution % 8 > 0 ? 1 : 0);
                BindMap(_insertRawDataToTexKernel, mapBuffer, chunkCoordMapSpace, mapSize);
                BindHeightmapAsDestination(_insertRawDataToTexKernel, heightmap, heightmapChunkResolution);
                _shader.SetBool(FlipXYWhenImportRawHeightmap, true);
                _shader.SetBuffer(_insertRawDataToTexKernel, ChunkHeightmap, chunkSourceData);
                _shader.Dispatch(_insertRawDataToTexKernel, treadGroups, treadGroups, 1);
            }
        }
        
        public void GetHeightmapForCollisionChunks(ComputeBuffer transportBuffer, ComputeBuffer requestBuffer,
            int collisionChunkResolution, int collisionChunksCount, float collisionChunkSize, RenderTexture heightmap,
            ComputeBuffer mapBuffer, Vector2Int mapMinChunk, int mapSize, int heightmapChunkResolution,
            float heightmapChunkSize, float heightmapChunkHeight, int heightmapChunksCountPerSide,
            Vector2 collisionOffset)
        {
            _shader.GetKernelThreadGroupSizes(_getCollisionMapKernel, out uint x, out uint y, out uint z);
            int chunkResPlusOne = collisionChunkResolution + 1;
            int treadGroupsX = (int)(chunkResPlusOne / x + (chunkResPlusOne % x > 0 ? 1 : 0));
            int treadGroupsY = (int)(chunkResPlusOne / y + (chunkResPlusOne % y > 0 ? 1 : 0));
            int treadGroupsZ = (int)(collisionChunksCount / z + (collisionChunksCount % z > 0 ? 1 : 0));
            BindMap(_getCollisionMapKernel, mapBuffer, mapMinChunk, mapSize);
            _shader.SetFloat(ChunkSize, heightmapChunkSize);
            _shader.SetFloat(ChunkHeight, heightmapChunkHeight);
            _shader.SetInt(CollisionChunkResolution, collisionChunkResolution);
            _shader.SetFloat(HeightmapPixelSizeUVSpace, 1f / (heightmapChunkResolution + 2));
            _shader.SetFloat(HeightmapPixelSize, heightmapChunkSize / heightmapChunkResolution);
            _shader.SetFloat(CollisionPixelSize, collisionChunkSize / collisionChunkResolution);
            _shader.SetFloat(SlotsCountInv, 1f / heightmapChunksCountPerSide);
            _shader.SetVector(CollisionOffset, collisionOffset);
            //_shader.SetFloat(CollisionChunkSize, collisionChunkSize);
            //_shader.SetFloat(CollisionToHeightmapMul, (float)heightmapChunkResolution / (collisionChunkResolution * collisionChunkResolution));
            BindHeightmapAsSource(_getCollisionMapKernel, heightmap, heightmapChunkResolution, mapMinChunk);
            _shader.SetBuffer(_getCollisionMapKernel, DestinationCollisionMap, transportBuffer);
            _shader.SetBuffer(_getCollisionMapKernel, CollisionMapRequest, requestBuffer);
            _shader.SetInt(CollisionChunksCount, collisionChunksCount);
            _shader.Dispatch(_getCollisionMapKernel, treadGroupsX, treadGroupsY, treadGroupsZ);
        }

        private void BindMap(int kernelIndex, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace, int mapSize)
        {
            _shader.SetInt(MapSize, mapSize);
            _shader.SetInt(ChunkCoordX, chunkCoordMapSpace.x);
            _shader.SetInt(ChunkCoordY, chunkCoordMapSpace.y);
            _shader.SetBuffer(kernelIndex, Map, mapBuffer);
        }

        private void BindHeightmapAsDestination(int kernelIndex, RenderTexture heightmap, int heightmapResolution)
        {
            _shader.SetInt(HeightmapChunkResolution, heightmapResolution);
            _shader.SetTexture(kernelIndex, DestinationHeightmap, heightmap);
        }
        
        private void BindHeightmapAsSource(int kernelIndex, RenderTexture heightmap, int heightmapResolution, Vector2Int minCoverage)
        {
            _shader.SetInt(HeightmapChunkResolution, heightmapResolution);
            _shader.SetInt(HeightmapStartX, minCoverage.x);
            _shader.SetInt(HeightmapStartY, minCoverage.y);
            _shader.SetTexture(kernelIndex, SourceHeightmap, heightmap);
        }
    }
}