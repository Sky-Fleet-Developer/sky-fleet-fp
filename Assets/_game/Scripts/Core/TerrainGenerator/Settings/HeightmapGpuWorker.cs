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
        private static readonly int HeightmapWidthCount = Shader.PropertyToID("heightmap_width_count");
        private static readonly int MapSize = Shader.PropertyToID("map_size");
        private static readonly int ChunkCoordX = Shader.PropertyToID("chunk_coord_x");
        private static readonly int ChunkCoordY = Shader.PropertyToID("chunk_coord_y");
        private static readonly int Map = Shader.PropertyToID("map");
        private static readonly int ChunkHeightmap = Shader.PropertyToID("chunk_heightmap");
        private readonly int _copyHeightmapKernel;
        private readonly int _applyDeformationKernel;
        private readonly int _alignVerticesToHeightmapKernel;
        private readonly int _insertRawDataToTexKernel;
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
                _shader.SetBuffer(_insertRawDataToTexKernel, ChunkHeightmap, chunkSourceData);
                _shader.Dispatch(_insertRawDataToTexKernel, treadGroups, treadGroups, 1);
            }
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
            _shader.SetInt(HeightmapWidthCount, heightmapResolution);
            _shader.SetTexture(kernelIndex, DestinationHeightmap, heightmap);
        }
        
        private void BindHeightmapAsSource(int kernelIndex, RenderTexture heightmap, int heightmapResolution, Vector2Int minCoverage)
        {
            _shader.SetInt(HeightmapWidthCount, heightmapResolution);
            _shader.SetInt(HeightmapStartX, minCoverage.x);
            _shader.SetInt(HeightmapStartY, minCoverage.y);
            _shader.SetTexture(kernelIndex, SourceHeightmap, heightmap);
        }
    }
}