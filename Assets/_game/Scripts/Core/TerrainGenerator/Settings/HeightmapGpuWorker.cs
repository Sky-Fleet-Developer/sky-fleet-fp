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
        private readonly int _copyHeightmapKernel;
        private readonly int _applyDeformationKernel;
        private readonly int _alignVerticesToHeightmapKernel;
        private readonly int _testAlignSineKernel;
        
        private ComputeShader _shader;
        private Dictionary<int, ComputeBuffer> _activeRectSettingsBuffers = new ();
        private int _rectSettingsBufferIndex = 0;
        private List<ComputeBuffer> _rectSettingsBuffersPool = new ();

        public HeightmapGpuWorker(ComputeShader shader)
        {
            _shader = shader;
            _copyHeightmapKernel = _shader.FindKernel("CSCopyHeightmap");
            _applyDeformationKernel = _shader.FindKernel("CSApplyDeformation");
            _alignVerticesToHeightmapKernel = _shader.FindKernel("CSAlignVerticesToHeightmap");
            _testAlignSineKernel = _shader.FindKernel("CSTestAlignSine");
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

        public void AlignVerticesToHeightmap(GraphicsBuffer vertexBuffer, ComputeBuffer heightmap, int resolution, float chunkSize, float chunkHeight)
        {
            AlignVerticesToHeightmap(vertexBuffer, heightmap, resolution, resolution, chunkSize, chunkHeight, Vector2Int.zero, new Vector2Int(resolution, resolution));
        }
        
        public void AlignVerticesToHeightmap(GraphicsBuffer vertexBuffer, ComputeBuffer heightmap, int resolution, int heightmapResolution, float chunkSize, float chunkHeight, Vector2Int minCoverage, Vector2Int maxCoverage)
        {
            lock (_shader)
            {
                //Debug.LogError($"AlignVerticesToHeightmap: vertices = {vertexBuffer.count}, heightmap = {heightmap.count}, {resolution}, {chunkSize}, {chunkHeight}");
                int treadGroups = vertexBuffer.count / 8 + (vertexBuffer.count % 8 > 0 ? 1 : 0);
                _shader.SetBuffer(_alignVerticesToHeightmapKernel, Vertices, vertexBuffer);
                _shader.SetBuffer(_alignVerticesToHeightmapKernel, SourceHeightmap, heightmap);
                _shader.SetInt(VerticesCount, vertexBuffer.count);
                _shader.SetInt(VerticesWidthCount, resolution + 1);
                _shader.SetInt(HeightmapWidthCount, heightmapResolution + 1);
                _shader.SetInt(HeightmapStartX, minCoverage.x);
                _shader.SetInt(HeightmapStartY, minCoverage.y);
                _shader.SetFloat(ChunkSize, chunkSize);
                _shader.SetFloat(ChunkHeight, chunkHeight);
                _shader.SetFloat(NormalDx, chunkSize / (resolution - 1) * 2);
                _shader.SetFloat(NormalDt, (resolution - 1) / chunkSize * 0.5f);

                _shader.Dispatch(_alignVerticesToHeightmapKernel, treadGroups, treadGroups, 1);
            }
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
    }
}