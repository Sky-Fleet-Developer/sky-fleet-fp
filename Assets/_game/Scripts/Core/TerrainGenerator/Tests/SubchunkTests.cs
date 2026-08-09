using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
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
            var subchunk = new Subchunk("test", root, 20, 1, 16, Vector2Int.zero, 1, Resources.Load<Material>("SubckunkTestMat"), null);
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
            var subchunk = new Subchunk("test", root, 20, 1, 16, Vector2Int.zero, 1, Resources.Load<Material>("SubckunkTestMat"), heightmapGpuWorker);
            await subchunk.GenerationTask;
            heightmapGpuWorker.TestAlignSine(subchunk.VertexBuffer, 16, 20);
            await Task.Delay(15000);
            Subchunk.ClearPool();
        }
    }
}