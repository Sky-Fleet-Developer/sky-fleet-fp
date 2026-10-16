using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core;
using Core.Scatter;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Runtime.Scatter
{
    /// <summary>
    /// What the kinds of scattered details (grass, small stones) share in the scene: the areas kept clear of them, and
    /// the ground covered by the kinds placed earlier in the frame (a grid of cells around the main camera).
    /// Put it on a scene root object, like the renderers of the kinds. The areas are uploaded once a frame, when the
    /// first kind binds them.
    /// </summary>
    public class ScatterSystem : MonoBehaviour, IMyInstaller, IScatter, IScatterGpuAreas, IScatterGpuCover
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct AreaData
        {
            public Matrix4x4 WorldToLocal;
            public Vector3 Size;
            public float Edge;
            public uint Shape;
            public uint PolygonStart;
            public uint PolygonCount;
        }

        /// <summary>Stride of ScatterArea (ScatterCommon.hlsl).</summary>
        public const int AreaStride = sizeof(float) * 23;

        private static readonly int AreasProperty = Shader.PropertyToID("scatter_areas");
        private static readonly int AreaCountProperty = Shader.PropertyToID("scatter_area_count");
        private static readonly int PolygonsProperty = Shader.PropertyToID("scatter_polygons");
        private static readonly int CoverProperty = Shader.PropertyToID("scatter_cover");
        private static readonly int CoverOriginProperty = Shader.PropertyToID("scatter_cover_origin");
        private static readonly int CoverCellProperty = Shader.PropertyToID("scatter_cover_cell");
        private static readonly int CoverSizeProperty = Shader.PropertyToID("scatter_cover_size");
        private static readonly int CoverStampProperty = Shader.PropertyToID("scatter_cover_stamp");
        private static readonly int CoverReadProperty = Shader.PropertyToID("scatter_cover_read");
        private static readonly int CoverWriteProperty = Shader.PropertyToID("scatter_cover_write");

        // Large objects (rocks), small details (stones)
        private const int CoverLayers = 2;

        // The stamp of a frame takes the high 24 bits of a cover cell
        private const uint MaxCoverStamp = (1u << 24) - 1;

        [Title("Cover")]
        [PropertyTooltip("Meters from the camera, along x and z, within which details covering the ground (stones) keep " +
                         "others (grass) away")]
        [SerializeField, MinValue(8)] private float coverRange = 64;
        [PropertyTooltip("Meters per cell of the cover grid. Smaller cells follow small stones closer, and take more memory")]
        [SerializeField, PropertyRange(0.05f, 1)] private float coverCellSize = 0.125f;

        private readonly List<IScatterArea> _areas = new ();
        private readonly List<AreaData> _areaData = new ();
        private readonly List<Vector2> _polygonPoints = new ();
        private GraphicsBuffer _areaBuffer;
        private GraphicsBuffer _polygonBuffer;
        private int _uploadedFrame = -1;
        private GraphicsBuffer _cover;
        private int _coverSize;
        private float _coverCell;
        private Vector2 _coverOrigin;
        private uint _coverStamp;
        private int _coverFrame = -1;

        public void InstallBindings(DiContainer container)
        {
            container.Bind(typeof(IScatter), typeof(IScatterGpuAreas), typeof(IScatterGpuCover)).FromInstance(this);
        }

        public void Bind(ComputeShader shader, int kernel, ScatterCoverUse use)
        {
            // Layers not written yet this frame hold earlier stamps, which read as uncovered
            BeginCoverFrame();
            int read = ((use & ScatterCoverUse.ReadLarge) != 0 ? 1 : 0) | ((use & ScatterCoverUse.ReadSmall) != 0 ? 2 : 0);
            int write = (use & ScatterCoverUse.WriteLarge) != 0 ? 0 : (use & ScatterCoverUse.WriteSmall) != 0 ? 1 : -1;
            shader.SetBuffer(kernel, CoverProperty, _cover);
            shader.SetVector(CoverOriginProperty, _coverOrigin);
            shader.SetFloat(CoverCellProperty, _coverCell);
            shader.SetInt(CoverSizeProperty, _coverSize);
            shader.SetInt(CoverStampProperty, (int)_coverStamp);
            shader.SetInt(CoverReadProperty, read);
            shader.SetInt(CoverWriteProperty, write);
        }

        /// <summary>Binds no cover, for kinds without a scatter system in the scene or not using the cover.</summary>
        public static void BindNoCover(ComputeShader shader, int kernel, GraphicsBuffer emptyCover)
        {
            shader.SetBuffer(kernel, CoverProperty, emptyCover);
            shader.SetInt(CoverSizeProperty, 0);
            shader.SetInt(CoverReadProperty, 0);
            shader.SetInt(CoverWriteProperty, -1);
        }

        // A new stamp and a grid centered on the camera, once a frame
        private void BeginCoverFrame()
        {
            if (_coverFrame == Time.frameCount && _cover != null)
            {
                return;
            }
            _coverFrame = Time.frameCount;

            _coverCell = Mathf.Max(0.02f, coverCellSize);
            int size = Mathf.Clamp(Mathf.CeilToInt(2 * coverRange / _coverCell), 8, 4096);
            if (_cover == null || size != _coverSize)
            {
                _cover?.Release();
                _coverSize = size;
                _cover = new GraphicsBuffer(GraphicsBuffer.Target.Structured, CoverLayers * size * size, sizeof(uint));
                ClearCover();
            }
            if (++_coverStamp > MaxCoverStamp)
            {
                // Once every 2^24 frames: older stamps would read as newer
                ClearCover();
            }

            Camera camera = Camera.main;
            Vector3 center = camera ? camera.transform.position : Vector3.zero;
            float half = _coverSize / 2 * _coverCell;
            _coverOrigin = new Vector2(Mathf.Floor(center.x / _coverCell) * _coverCell - half,
                Mathf.Floor(center.z / _coverCell) * _coverCell - half);
        }

        private void ClearCover()
        {
            _cover.SetData(new uint[CoverLayers * _coverSize * _coverSize]);
            _coverStamp = 1;
        }

        public void AddArea(IScatterArea area)
        {
            if (area != null && !_areas.Contains(area))
            {
                _areas.Add(area);
            }
        }

        public void RemoveArea(IScatterArea area) => _areas.Remove(area);

        public void Bind(ComputeShader shader, int kernel)
        {
            if (_uploadedFrame != Time.frameCount || _areaBuffer == null)
            {
                _uploadedFrame = Time.frameCount;
                Upload();
            }
            shader.SetBuffer(kernel, AreasProperty, _areaBuffer);
            shader.SetBuffer(kernel, PolygonsProperty, _polygonBuffer);
            shader.SetInt(AreaCountProperty, _areaData.Count);
        }

        /// <summary>Binds no areas, for kinds without a scatter system in the scene.</summary>
        public static void BindNone(ComputeShader shader, int kernel, GraphicsBuffer emptyAreas, GraphicsBuffer emptyPolygons)
        {
            shader.SetBuffer(kernel, AreasProperty, emptyAreas);
            shader.SetBuffer(kernel, PolygonsProperty, emptyPolygons);
            shader.SetInt(AreaCountProperty, 0);
        }

        private void Upload()
        {
            _areaData.Clear();
            _polygonPoints.Clear();
            _areas.RemoveAll(area => area is Object destroyed && !destroyed);
            foreach (IScatterArea area in _areas)
            {
                ScatterAreaData data = area.Area;
                int polygonStart = _polygonPoints.Count;
                int polygonCount = 0;
                if (data.Shape == ScatterAreaShape.Polygon)
                {
                    if (data.Polygon == null || data.Polygon.Count < 3)
                    {
                        continue;
                    }
                    _polygonPoints.AddRange(data.Polygon);
                    polygonCount = data.Polygon.Count;
                }

                _areaData.Add(new AreaData
                {
                    WorldToLocal = data.WorldToLocal,
                    Size = data.Size,
                    Edge = Mathf.Max(0, data.Edge),
                    Shape = (uint)data.Shape,
                    PolygonStart = (uint)polygonStart,
                    PolygonCount = (uint)polygonCount,
                });
            }

            // At least one element: every buffer a kernel reads must be bound
            ScatterUtility.EnsureBuffer(ref _areaBuffer, Mathf.Max(1, _areaData.Count), AreaStride);
            ScatterUtility.EnsureBuffer(ref _polygonBuffer, Mathf.Max(1, _polygonPoints.Count), sizeof(float) * 2);
            if (_areaData.Count > 0)
            {
                _areaBuffer.SetData(_areaData);
            }
            if (_polygonPoints.Count > 0)
            {
                _polygonBuffer.SetData(_polygonPoints);
            }
        }

        private void OnDestroy()
        {
            _areaBuffer?.Release();
            _polygonBuffer?.Release();
            _cover?.Release();
        }
    }
}
