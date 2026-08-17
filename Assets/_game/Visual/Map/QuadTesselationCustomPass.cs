using System;
using System.Collections.Generic;
using Core.TerrainGenerator;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[Serializable]
class QuadTesselationCustomPass : CustomPass
{
    [SerializeField] private TerrainProvider terrainProvider;
    private static Mesh _quadMesh;

    private HeightmapData _heightmapData;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (!terrainProvider)
        {
            return;
        }
        _heightmapData = terrainProvider.GetHeightmapData();
        TerrainGenerationSettings settings = terrainProvider.settings;
        if (!_quadMesh)
        {
            CreateMesh(settings.useQuadsInsteadOfTriangles, settings.ChunkSize, settings.Height);
        }
    }

    private static void CreateMesh(bool useQuadsInsteadOfTriangles, float width, float height)
    {
        _quadMesh = new Mesh();
        _quadMesh.name = "TerrainQuad";
        int indexCount = useQuadsInsteadOfTriangles ? 4 : 6;
        SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
        subMeshDescriptor.baseVertex = 0;
        subMeshDescriptor.firstVertex = 0;
        subMeshDescriptor.indexCount = indexCount;
        subMeshDescriptor.indexStart = 0;
        subMeshDescriptor.topology = useQuadsInsteadOfTriangles ? MeshTopology.Quads : MeshTopology.Triangles;
        subMeshDescriptor.vertexCount = 4;
        subMeshDescriptor.bounds = new Bounds(Vector3.zero, Vector3.one);
        List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>(1) { subMeshDescriptor };

        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };
        _quadMesh.SetVertexBufferParams(4, layout);
        _quadMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        PackedVertex[] initVertexData = new PackedVertex[]
        {
            new(new float3(0, 0, 0), new float3(0, 1, 0), new float2(0, 0)),
            new(new float3(0, 0, width), new float3(0, 1, 0), new float2(0, 1)),
            new(new float3(width, 0, width), new float3(0, 1, 0), new float2(1, 1)),
            new(new float3(width, 0, 0), new float3(0, 1, 0), new float2(1, 0)),
        };
        int[] indices = useQuadsInsteadOfTriangles ? new int[] { 0, 1, 2, 3 } : new int[] { 0, 1, 2, 2, 3, 0 };

        _quadMesh.SetVertexBufferData(initVertexData, 0, 0, 4, 0,
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
            MeshUpdateFlags.DontValidateIndices);
        _quadMesh.SetIndexBufferData(indices, 0, 0, indexCount,
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
            MeshUpdateFlags.DontValidateIndices);
        Vector3 boundsSize = new Vector3(width, height, width);
        _quadMesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
        _quadMesh.SetSubMeshes(subMeshes,
            MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontResetBoneBounds);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!terrainProvider)
        {
            return;
        }
        foreach (var activeChunk in terrainProvider.GetActiveChunks())
        {
            var matrix = activeChunk.transform.localToWorldMatrix;
            ctx.cmd.DrawMesh(_quadMesh, matrix, activeChunk.Material, 0);
        }
    }

    protected override void Cleanup()
    {
        if (_quadMesh)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_quadMesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_quadMesh);
            }
            _quadMesh = null;
        }
    }
}