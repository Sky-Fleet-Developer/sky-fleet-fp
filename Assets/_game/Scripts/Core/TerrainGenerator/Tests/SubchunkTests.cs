using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator.Tests
{
    [TestFixture(TestOf = typeof(Subchunk))]
    public class SubchunkTests
    {

        [Test]
        public async Task Test_CreateMesh()
        {
            Transform root = new GameObject("[TestSubchunk]").transform;
            var subchunk = new Subchunk("test", root, 20, 1, 16, 16, Vector2Int.zero, 1, Resources.Load<Material>("SubckunkTestMat"), null);
            await subchunk.GenerationTask;
            await Task.Delay(15000);
            Subchunk.ClearPool();
        }
        
        [Test]
        public async Task Test_Create_AlignSine()
        {
            Transform root = new GameObject("[TestSubchunk]").transform;
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/_game/Scripts/Core/TerrainGenerator/Shader/HeightmapWorker.compute");
            Debug.Assert(shader != null);
            HeightmapGpuWorker heightmapGpuWorker = new(shader);
            var subchunk = new Subchunk("test", root, 20, 1, 16, 16, Vector2Int.zero, 1, Resources.Load<Material>("SubckunkTestMat"), heightmapGpuWorker);
            await subchunk.GenerationTask;
            heightmapGpuWorker.TestAlignSine(subchunk.VertexBuffer, 16, 20);
            await Task.Delay(15000);
            Subchunk.ClearPool();
        }

        [Test]
        public async Task Test_Create_AndAlignByHeightmap()
        {
            var data = await RawReader.ReadAsync("Landscapes/10x10-8x8/10x10-8x8Height_1-5.r16");
            ComputeBuffer heightmapBuffer = new ComputeBuffer(data.Length, sizeof(float));
            heightmapBuffer.SetData(data);
            
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/_game/Scripts/Core/TerrainGenerator/Shader/HeightmapWorker.compute");
            HeightmapGpuWorker heightmapGpuWorker = new(shader);
            Transform root = new GameObject("[TestSubchunk]").transform;
            var subchunk = new Subchunk("test", root, 30, 10, data.GetLength(0)-1, data.GetLength(0)-1, Vector2Int.zero, 1, Resources.Load<Material>("SubckunkTestMat"), heightmapGpuWorker);
            
            await subchunk.GenerationTask;
            heightmapGpuWorker.AlignVerticesToHeightmap(subchunk.VertexBuffer, heightmapBuffer, data.GetLength(0)-1, 30, 10);
            await Task.Delay(15000);
            heightmapBuffer.Dispose();
            Subchunk.ClearPool();
        }
    }
}