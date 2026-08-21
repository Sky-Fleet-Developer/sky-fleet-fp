using System;
using System.Collections.Generic;
using Core.TerrainGenerator;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using TerrainData = Core.TerrainGenerator.TerrainData;

[Serializable]
class QuadTesselationCustomPass : CustomPass
{
    [SerializeField] private TerrainProvider terrainProvider;
    [SerializeField] private bool drawWireMesh;
    [SerializeField] private Pass pass;
    private Material _sourceMaterial;
    private static Mesh _quadMesh;

    private enum Pass
    {
        ShadowCaster,
        DepthPrepass,
        GBuffer,
        MotionVectors
    }
    
    private TerrainData _terrainData;

    private int _gBufferPass;
    private int _shadowCasterPass;
    private int _motionVectorsPass;
    private int _depthPrepassPass;

    //private static readonly ShaderTagId gBufferPassTag = new ShaderTagId("GBuffer");
    //private static readonly ShaderTagId forwardLitPassTag = new ShaderTagId("Forward");
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
        _terrainData = terrainProvider.GetHeightmapData();
        TerrainGenerationSettings settings = terrainProvider.Settings;
        if (!_quadMesh)
        {
            CreateMesh(settings.ChunkMeshResolution, settings.ChunkMeshSize, settings.Height);
        }

        _sourceMaterial = settings.Material;
        _gBufferPass = _sourceMaterial.FindPass("GBuffer");
        //_forwardLitPass = _sourceMaterial.FindPass("Forward");
        _shadowCasterPass = _sourceMaterial.FindPass("ShadowCaster");
        _depthPrepassPass = _sourceMaterial.FindPass("DepthPrepass");
        _motionVectorsPass = _sourceMaterial.FindPass("MotionVectors");
    }
    
    protected override void Execute(CustomPassContext ctx)
    {
        if (!terrainProvider)
        {
            return;
        }


        if (drawWireMesh)
        {
            ctx.cmd.SetWireframe(true);
            foreach (var activeChunk in terrainProvider.GetActiveChunks())
            {
                var matrix = activeChunk.transform.localToWorldMatrix;
                Draw(ctx, matrix, activeChunk);
            }
            ctx.cmd.SetWireframe(false);
        }
        else
        {
            foreach (var activeChunk in terrainProvider.GetActiveChunks())
            {
                var matrix = activeChunk.transform.localToWorldMatrix;
                Draw(ctx, matrix, activeChunk);
            }
        }
    }

    private void Draw(CustomPassContext ctx, Matrix4x4 matrix, IChunk activeChunk)
    {
        switch (pass)
        {
            case Pass.ShadowCaster:
                ctx.cmd.DrawMesh(_quadMesh, matrix, activeChunk.Material, 0, _shadowCasterPass);
                break;
            case Pass.DepthPrepass:
                ctx.cmd.DrawMesh(_quadMesh, matrix, activeChunk.Material, 0, _depthPrepassPass);
                break;
            case Pass.GBuffer:
                ctx.cmd.DrawMesh(_quadMesh, matrix, activeChunk.Material, 0, _gBufferPass);
                break;
            case Pass.MotionVectors:
                ctx.cmd.DrawMesh(_quadMesh, matrix, activeChunk.Material, 0, _motionVectorsPass);
                break;
        }
    }


    private static void CreateMesh(int resolution, float width, float height)
    {
        _quadMesh = new Mesh();
        _quadMesh.name = "TerrainQuad";
        int indexCount = resolution * resolution * 4;
        int verticesPerSide = resolution + 1;
        int vertexCount = verticesPerSide * verticesPerSide;
        SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
        subMeshDescriptor.baseVertex = 0;
        subMeshDescriptor.firstVertex = 0;
        subMeshDescriptor.indexCount = indexCount;
        subMeshDescriptor.indexStart = 0;
        subMeshDescriptor.topology = MeshTopology.Quads;
        subMeshDescriptor.vertexCount = vertexCount;
        subMeshDescriptor.bounds = new Bounds(Vector3.zero, Vector3.one);
        List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>(1) { subMeshDescriptor };

        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };
        _quadMesh.SetVertexBufferParams(vertexCount, layout);
        _quadMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        float step = width / resolution;
        float resInv = 1f / resolution;
        PackedVertex[] vertices = new PackedVertex[vertexCount];
        int[] indices = new int[indexCount];
        int tIndex = 0;
        for (int j = 0; j <= resolution; j++)
        {
            for (int i = 0; i <= resolution; i++)
            {
                int vIndex = i * verticesPerSide + j;

                PackedVertex vert = new PackedVertex();
                vert.Position = new float3(j * step, 0, i * step);
                vert.Normal = new float3(0, 1, 0);
                vert.UV = new float2(j * resInv, i * resInv);
                vertices[vIndex] = vert;

                if (i < resolution && j < resolution)
                {
                    // Индексы четырех вершин текущего квадрата
                    int bottomLeft = vIndex;
                    int bottomRight = vIndex + 1;
                    int topLeft = vIndex + verticesPerSide;
                    int topRight = vIndex + verticesPerSide + 1;

                    indices[tIndex++] = bottomLeft;
                    indices[tIndex++] = topLeft;
                    indices[tIndex++] = topRight;
                    indices[tIndex++] = bottomRight;
                }
            }
        }

        _quadMesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
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