using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Scatter;
using Core.TerrainGenerator;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime.Scatter
{

    /// <summary>
    /// Placement and drawing of one kind of scattered detail, all on the GPU (see Visual/Scatter/ScatterPlacement.hlsl).
    /// Every frame the placement shader of the kind culls tiles around the camera (range, frustum, hills in the way)
    /// and places instances on the terrain mesh, of the layers of the surface types under them, out of the areas kept
    /// clear. Instances are drawn with Graphics.RenderMeshIndirect in batches; a batch mesh holds its shape once for
    /// every instance of its part of the instance buffer (<see cref="ScatterBatchMesh"/>) and the placement sets how
    /// many are drawn. The kind sets its own shader parameters on <see cref="Shader"/> before <see cref="Place"/>, and
    /// its own material parameters before <see cref="Draw"/>.
    /// </summary>
    public class ScatterPipeline : IDisposable
    {
        /// <summary>A batch to build: a shape and how many of it are drawn at most.</summary>
        public struct BatchSource
        {
            public Mesh Shape;
            public int Capacity;
            /// <summary>Drawn closer than <see cref="Settings.DetailedDistance"/>; may cast shadows.</summary>
            public bool Near;
        }

        /// <summary>Placement settings of the kind, per frame.</summary>
        public struct Settings
        {
            /// <summary>Meters from the camera where the details end.</summary>
            public float Range;
            /// <summary>Part of the range over which they fade out.</summary>
            public float Fade;
            /// <summary>Meters within which they have their full density; farther it falls with the square of the distance.</summary>
            public float FullDensityDistance;
            /// <summary>Details closer use the near batches.</summary>
            public float DetailedDistance;
            /// <summary>Tallest detail in meters, for the bounds of the tiles.</summary>
            public float MaxHeight;
            public float TileSize;
            public bool TerrainOcclusion;
            public int OcclusionSteps;
            /// <summary>Near details cast shadows.</summary>
            public bool CastShadows;
        }

        public class Batch
        {
            public Mesh Mesh;
            public bool Near;
            public int Offset;
            public int Capacity;
            public int IndicesPerInstance;
            public MaterialPropertyBlock Properties;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BatchData
        {
            public const int Stride = sizeof(uint) * 4;

            public uint Offset;
            public uint Capacity;
            public uint IndicesPerInstance;
            public uint Padding;
        }

        // Thread groups of a dispatch dimension are limited to 65535: one per visible tile
        private const int MaxTilesSide = 240;

        private static readonly int CameraProperty = UnityEngine.Shader.PropertyToID("scatter_camera");
        private static readonly int FrustumProperty = UnityEngine.Shader.PropertyToID("scatter_frustum");
        private static readonly int RangeProperty = UnityEngine.Shader.PropertyToID("scatter_range");
        private static readonly int FadeProperty = UnityEngine.Shader.PropertyToID("scatter_fade");
        private static readonly int FullDensityDistanceProperty = UnityEngine.Shader.PropertyToID("scatter_full_density_distance");
        private static readonly int DetailedDistanceProperty = UnityEngine.Shader.PropertyToID("scatter_detailed_distance");
        private static readonly int MaxHeightProperty = UnityEngine.Shader.PropertyToID("scatter_max_height");
        private static readonly int TileSizeProperty = UnityEngine.Shader.PropertyToID("scatter_tile_size");
        private static readonly int TilesSideProperty = UnityEngine.Shader.PropertyToID("scatter_tiles_side");
        private static readonly int OcclusionProperty = UnityEngine.Shader.PropertyToID("scatter_occlusion");
        private static readonly int OcclusionStepsProperty = UnityEngine.Shader.PropertyToID("scatter_occlusion_steps");
        private static readonly int ShadowsProperty = UnityEngine.Shader.PropertyToID("scatter_shadows");
        private static readonly int BatchesProperty = UnityEngine.Shader.PropertyToID("scatter_batches");
        private static readonly int BatchCountProperty = UnityEngine.Shader.PropertyToID("scatter_batch_count");
        private static readonly int DispatchArgsProperty = UnityEngine.Shader.PropertyToID("scatter_dispatch_args");
        private static readonly int DrawArgsProperty = UnityEngine.Shader.PropertyToID("scatter_draw_args");
        private static readonly int VisibleTilesProperty = UnityEngine.Shader.PropertyToID("scatter_visible_tiles");
        private static readonly int InstancesProperty = UnityEngine.Shader.PropertyToID("scatter_instances");
        private static readonly int InstanceOffsetProperty = UnityEngine.Shader.PropertyToID("scatter_instance_offset");
        private static readonly int VerticesPerInstanceProperty = UnityEngine.Shader.PropertyToID("scatter_vertices_per_instance");

        private readonly int _instanceStride;
        private readonly bool _withUVs;
        private readonly int _resetKernel;
        private readonly int _cullKernel;
        private readonly int _clampKernel;
        private readonly List<Batch> _batches = new ();
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustum = new Vector4[6];
        private GraphicsBuffer _batchBuffer;
        private GraphicsBuffer _dispatchArgs;
        private GraphicsBuffer _drawArgs;
        private GraphicsBuffer _visibleTiles;
        private GraphicsBuffer _instances;
        private GraphicsBuffer _emptyAreas;
        private GraphicsBuffer _emptyPolygons;
        private GraphicsBuffer _emptyCover;

        /// <param name="shader">Placement shader of the kind, built on ScatterPlacement.hlsl.</param>
        /// <param name="instanceStride">Bytes of one instance of the kind (its scatter_instances element).</param>
        /// <param name="withUVs">The batch meshes keep UV0 and tangents of the shapes, for textured kinds.</param>
        public ScatterPipeline(ComputeShader shader, int instanceStride, bool withUVs = false)
        {
            Shader = shader;
            _instanceStride = instanceStride;
            _withUVs = withUVs;
            _resetKernel = shader.FindKernel("Reset");
            _cullKernel = shader.FindKernel("CullTiles");
            PlaceKernel = shader.FindKernel("Place");
            _clampKernel = shader.FindKernel("ClampArgs");
            _dispatchArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured, 3, sizeof(uint));
        }

        public ComputeShader Shader { get; }

        /// <summary>Kernel of the kind's own placement: bind its buffers to it.</summary>
        public int PlaceKernel { get; }

        /// <summary>In the order of <see cref="SetBatches"/>.</summary>
        public IReadOnlyList<Batch> Batches => _batches;

        /// <summary>Builds the batch meshes and the instance buffer. Costly: call it only when the shapes change.</summary>
        public void SetBatches(IReadOnlyList<BatchSource> sources, string kindName)
        {
            foreach (Batch batch in _batches)
            {
                ScatterUtility.DestroyObject(batch.Mesh);
            }
            _batches.Clear();

            int offset = 0;
            foreach (BatchSource source in sources)
            {
                int capacity = ScatterBatchMesh.FitInstances(source.Shape, source.Capacity);
                string name = $"{kindName} batch ({source.Shape.name}, {(source.Near ? "near" : "far")})";
                var batch = new Batch
                {
                    Mesh = ScatterBatchMesh.CreateBatch(source.Shape, capacity, name, _withUVs),
                    Near = source.Near,
                    Offset = offset,
                    Capacity = capacity,
                    IndicesPerInstance = ScatterBatchMesh.IndicesPerInstance(source.Shape),
                    Properties = new MaterialPropertyBlock(),
                };
                batch.Properties.SetFloat(InstanceOffsetProperty, offset);
                batch.Properties.SetFloat(VerticesPerInstanceProperty, source.Shape.vertexCount);
                _batches.Add(batch);
                offset += capacity;
            }

            var batchData = new BatchData[Mathf.Max(1, _batches.Count)];
            for (int i = 0; i < _batches.Count; i++)
            {
                batchData[i] = new BatchData
                {
                    Offset = (uint)_batches[i].Offset,
                    Capacity = (uint)_batches[i].Capacity,
                    IndicesPerInstance = (uint)_batches[i].IndicesPerInstance,
                };
            }
            ScatterUtility.EnsureBuffer(ref _batchBuffer, batchData.Length, BatchData.Stride);
            _batchBuffer.SetData(batchData);

            ScatterUtility.EnsureBuffer(ref _instances, Mathf.Max(1, offset), _instanceStride);
            foreach (Batch batch in _batches)
            {
                batch.Properties.SetBuffer(InstancesProperty, _instances);
            }

            // Index counts are written by the placement; each batch is drawn once
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[batchData.Length];
            for (int i = 0; i < args.Length; i++)
            {
                args[i].instanceCount = 1;
            }
            _drawArgs?.Release();
            _drawArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured,
                args.Length, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _drawArgs.SetData(args);
        }

        /// <param name="areas">Null: no areas.</param>
        /// <param name="cover">Null: no cover.</param>
        public void Place(Camera camera, in Settings settings, ITerrainGpuData terrain, ScatterLayerTable layers,
            IScatterGpuAreas areas, IScatterGpuCover cover = null, ScatterCoverUse coverUse = ScatterCoverUse.None)
        {
            if (_batches.Count == 0)
            {
                return;
            }

            float range = settings.Range;
            // Tiles grow when the range would need more of them than a dispatch takes
            float tileSize = Mathf.Max(settings.TileSize, 2 * range / (MaxTilesSide - 2));
            int tilesSide = Mathf.CeilToInt(2 * range / tileSize) + 2;
            if (_visibleTiles == null || _visibleTiles.count < tilesSide * tilesSide)
            {
                ScatterUtility.EnsureBuffer(ref _visibleTiles, tilesSide * tilesSide, sizeof(int) * 4);
            }

            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            for (int i = 0; i < _planes.Length; i++)
            {
                _frustum[i] = new Vector4(_planes[i].normal.x, _planes[i].normal.y, _planes[i].normal.z, _planes[i].distance);
            }

            ComputeShader shader = Shader;
            terrain.Bind(shader, _cullKernel);
            terrain.Bind(shader, PlaceKernel);
            layers.Bind(shader, PlaceKernel);
            if (areas != null)
            {
                areas.Bind(shader, PlaceKernel);
            }
            else
            {
                ScatterUtility.EnsureBuffer(ref _emptyAreas, 1, ScatterSystem.AreaStride);
                ScatterUtility.EnsureBuffer(ref _emptyPolygons, 1, sizeof(float) * 2);
                ScatterSystem.BindNone(shader, PlaceKernel, _emptyAreas, _emptyPolygons);
            }
            if (cover != null && coverUse != ScatterCoverUse.None)
            {
                cover.Bind(shader, PlaceKernel, coverUse);
            }
            else
            {
                ScatterUtility.EnsureBuffer(ref _emptyCover, 1, sizeof(uint));
                ScatterSystem.BindNoCover(shader, PlaceKernel, _emptyCover);
            }

            shader.SetVector(CameraProperty, camera.transform.position);
            shader.SetVectorArray(FrustumProperty, _frustum);
            shader.SetFloat(RangeProperty, range);
            shader.SetFloat(FadeProperty, settings.Fade);
            shader.SetFloat(FullDensityDistanceProperty, settings.FullDensityDistance);
            shader.SetFloat(DetailedDistanceProperty, settings.DetailedDistance);
            shader.SetFloat(MaxHeightProperty, settings.MaxHeight);
            shader.SetFloat(TileSizeProperty, tileSize);
            shader.SetInt(TilesSideProperty, tilesSide);
            shader.SetFloat(OcclusionProperty, settings.TerrainOcclusion ? 1 : 0);
            shader.SetInt(OcclusionStepsProperty, settings.OcclusionSteps);
            shader.SetFloat(ShadowsProperty, settings.CastShadows ? 1 : 0);
            shader.SetInt(BatchCountProperty, _batches.Count);

            shader.SetBuffer(_resetKernel, DispatchArgsProperty, _dispatchArgs);
            shader.SetBuffer(_resetKernel, DrawArgsProperty, _drawArgs);
            shader.SetBuffer(_cullKernel, DispatchArgsProperty, _dispatchArgs);
            shader.SetBuffer(_cullKernel, VisibleTilesProperty, _visibleTiles);
            shader.SetBuffer(PlaceKernel, VisibleTilesProperty, _visibleTiles);
            shader.SetBuffer(PlaceKernel, DrawArgsProperty, _drawArgs);
            shader.SetBuffer(PlaceKernel, InstancesProperty, _instances);
            shader.SetBuffer(PlaceKernel, BatchesProperty, _batchBuffer);
            shader.SetBuffer(_clampKernel, DrawArgsProperty, _drawArgs);
            shader.SetBuffer(_clampKernel, BatchesProperty, _batchBuffer);

            int batchGroups = Mathf.CeilToInt(_batches.Count / 64f);
            shader.Dispatch(_resetKernel, batchGroups, 1, 1);
            int groups = Mathf.CeilToInt(tilesSide / 8f);
            shader.Dispatch(_cullKernel, groups, groups, 1);
            shader.DispatchIndirect(PlaceKernel, _dispatchArgs, 0);
            shader.Dispatch(_clampKernel, batchGroups, 1, 1);
        }

        /// <summary>
        /// Draws every batch with the material of the renderParams; near and far batches cast shadows when asked.
        /// Only near details are placed outside the view for their shadows (<see cref="Settings.CastShadows"/>).
        /// </summary>
        public void Draw(RenderParams renderParams, bool nearShadows, bool farShadows = false)
        {
            for (int i = 0; i < _batches.Count; i++)
            {
                Batch batch = _batches[i];
                renderParams.matProps = batch.Properties;
                bool shadows = batch.Near ? nearShadows : farShadows;
                renderParams.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                Graphics.RenderMeshIndirect(renderParams, batch.Mesh, _drawArgs, 1, i);
            }
        }

        public void Dispose()
        {
            foreach (Batch batch in _batches)
            {
                ScatterUtility.DestroyObject(batch.Mesh);
            }
            _batches.Clear();
            _batchBuffer?.Release();
            _dispatchArgs?.Release();
            _drawArgs?.Release();
            _visibleTiles?.Release();
            _instances?.Release();
            _emptyAreas?.Release();
            _emptyPolygons?.Release();
            _emptyCover?.Release();
        }
    }
}
