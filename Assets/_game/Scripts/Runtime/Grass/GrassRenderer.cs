using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Core;
using Core.Grass;
using Core.TerrainGenerator;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

namespace Runtime.Grass
{
    /// <summary>
    /// Grass on the terrain, all on the GPU. Every frame GrassPlacement.compute culls tiles around the main camera
    /// (range, frustum, hills in the way) and places plants on the terrain mesh, of the grass layers of the surface
    /// types under them (mixed by their densities and density patches), out of the areas without grass and bent by the
    /// pushers. Plants are drawn with Graphics.RenderMeshIndirect in batches,
    /// one per shape and distance (near, far); a batch mesh holds its shape once for every plant of its part of the
    /// plant buffer (GrassBladeMesh) and the placement sets how many are drawn.
    /// Past the range the terrain takes the grass color (ITerrainFarTint).
    /// </summary>
    public class GrassRenderer : MonoBehaviour, IMyInstaller, IGrass
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 22;

            public float Density;
            public Vector2 Height;
            public float Width;
            public float Bend;
            public float ColorVariation;
            public Vector3 RootColor;
            public Vector3 TipColor;
            public uint Shape;
            public Vector4 Noise;
            public uint NoiseSeed;
            public uint Part0;
            public uint Part1;
            public uint Part2;
            public uint Part3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BatchData
        {
            public const int Stride = sizeof(uint) * 4;

            public uint Offset;
            public uint Capacity;
            public uint IndicesPerPlant;
            public uint Padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AreaData
        {
            public const int Stride = sizeof(float) * 23;

            public Matrix4x4 WorldToLocal;
            public Vector3 Size;
            public float Edge;
            public uint Shape;
            public uint PolygonStart;
            public uint PolygonCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PusherData
        {
            public const int Stride = sizeof(float) * 5;

            public Vector3 Center;
            public float Radius;
            public float Strength;
        }

        private class Batch
        {
            public Mesh Mesh;
            public bool Near;
            public int Offset;
            public int Capacity;
            public int IndicesPerPlant;
            public MaterialPropertyBlock Properties;
        }

        private const int BladeStride = sizeof(float) * 14;
        // Thread groups of a dispatch dimension are limited to 65535: one per visible tile
        private const int MaxTilesSide = 240;
        // Values of the density noise above which the given share of the ground lies, for shares 0, 0.1 .. 1
        private static readonly float[] NoiseThresholds =
            { 1.000f, 0.724f, 0.653f, 0.598f, 0.548f, 0.500f, 0.452f, 0.403f, 0.347f, 0.276f, 0.000f };

        [SerializeField] private GrassSettings settings;
        [SerializeField] private ComputeShader placement;
        [PropertyTooltip("Grass.shadergraph")]
        [SerializeField] private Shader bladeShader;
        [SerializeField] private bool drawGrass = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private ITerrainFarTint _farTint;

        private readonly List<IGrassArea> _areas = new ();
        private readonly List<IGrassPusher> _pushers = new ();
        private readonly List<AreaData> _areaData = new ();
        private readonly List<Vector2> _polygonPoints = new ();
        private readonly List<PusherData> _pusherData = new ();

        // Shape 0 is the built-in blade
        private readonly List<GrassShape> _shapes = new () { null };
        private readonly List<GrassShape> _subscribedShapes = new ();
        private readonly List<Batch> _batches = new ();
        private readonly HashSet<GrassShape> _reportedShapes = new ();
        private string _geometryKey;
        private ITerrainGpuData _subscribedTerrain;
        private GrassSettings _subscribedSettings;
        private bool _surfacesDirty = true;
        private Material _material;
        private Mesh _builtInBlade;
        private Mesh _builtInFarBlade;
        private int _builtInSegments;
        private GraphicsBuffer _layers;
        private GraphicsBuffer _surfaceLayers;
        private GraphicsBuffer _palette;
        private GraphicsBuffer _farTintColors;
        private GraphicsBuffer _batchBuffer;
        private GraphicsBuffer _areaBuffer;
        private GraphicsBuffer _polygonBuffer;
        private GraphicsBuffer _pusherBuffer;
        private GraphicsBuffer _dispatchArgs;
        private GraphicsBuffer _drawArgs;
        private GraphicsBuffer _visibleTiles;
        private GraphicsBuffer _blades;
        private float _maxDensity;
        private float _maxHeight;
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustum = new Vector4[6];
        private int _resetKernel;
        private int _cullKernel;
        private int _placeKernel;
        private int _clampKernel;
        private bool _farTintSet;

        private static readonly int LayersProperty = Shader.PropertyToID("grass_layers");
        private static readonly int SurfaceLayers = Shader.PropertyToID("grass_surface_layers");
        private static readonly int Palette = Shader.PropertyToID("grass_palette");
        private static readonly int SurfaceCount = Shader.PropertyToID("grass_surface_count");
        private static readonly int MaxDensity = Shader.PropertyToID("grass_max_density");
        private static readonly int CameraProperty = Shader.PropertyToID("grass_camera");
        private static readonly int Frustum = Shader.PropertyToID("grass_frustum");
        private static readonly int RangeProperty = Shader.PropertyToID("grass_range");
        private static readonly int FadeProperty = Shader.PropertyToID("grass_fade");
        private static readonly int FullDensityDistance = Shader.PropertyToID("grass_full_density_distance");
        private static readonly int MaxWidening = Shader.PropertyToID("grass_max_widening");
        private static readonly int DetailedDistance = Shader.PropertyToID("grass_detailed_distance");
        private static readonly int MaxHeight = Shader.PropertyToID("grass_max_height");
        private static readonly int TileSize = Shader.PropertyToID("grass_tile_size");
        private static readonly int TilesSide = Shader.PropertyToID("grass_tiles_side");
        private static readonly int Occlusion = Shader.PropertyToID("grass_occlusion");
        private static readonly int OcclusionSteps = Shader.PropertyToID("grass_occlusion_steps");
        private static readonly int Shadows = Shader.PropertyToID("grass_shadows");
        private static readonly int Batches = Shader.PropertyToID("grass_batches");
        private static readonly int BatchCount = Shader.PropertyToID("grass_batch_count");
        private static readonly int Areas = Shader.PropertyToID("grass_areas");
        private static readonly int AreaCount = Shader.PropertyToID("grass_area_count");
        private static readonly int Polygons = Shader.PropertyToID("grass_polygons");
        private static readonly int Pushers = Shader.PropertyToID("grass_pushers");
        private static readonly int PusherCount = Shader.PropertyToID("grass_pusher_count");
        private static readonly int DispatchArgs = Shader.PropertyToID("grass_dispatch_args");
        private static readonly int DrawArgs = Shader.PropertyToID("grass_draw_args");
        private static readonly int VisibleTiles = Shader.PropertyToID("grass_visible_tiles");
        private static readonly int Blades = Shader.PropertyToID("grass_blades");
        private static readonly int BladeOffset = Shader.PropertyToID("grass_blade_offset");
        private static readonly int VerticesPerPlant = Shader.PropertyToID("grass_vertices_per_plant");
        private static readonly int Wind = Shader.PropertyToID("grass_wind");
        private static readonly int WindShape = Shader.PropertyToID("grass_wind_shape");
        private static readonly int Look = Shader.PropertyToID("grass_look");

        public bool Enabled
        {
            get => drawGrass;
            set => drawGrass = value;
        }

        public GrassSettings Settings => settings;

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IGrass>().FromInstance(this);
        }

        public void AddArea(IGrassArea area)
        {
            if (area != null && !_areas.Contains(area))
            {
                _areas.Add(area);
            }
        }

        public void RemoveArea(IGrassArea area) => _areas.Remove(area);

        public void AddPusher(IGrassPusher pusher)
        {
            if (pusher != null && !_pushers.Contains(pusher))
            {
                _pushers.Add(pusher);
            }
        }

        public void RemovePusher(IGrassPusher pusher) => _pushers.Remove(pusher);

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (!drawGrass || !settings || !placement || !bladeShader || _terrain == null || !_terrain.IsReady || !camera)
            {
                ClearFarTint();
                return;
            }

            Subscribe();
            EnsureResources();
            if (_surfacesDirty)
            {
                BuildSurfaces();
                BuildGeometryWhenChanged();
            }
            if (_maxDensity <= 0)
            {
                ClearFarTint();
                return;
            }

            UploadAreasAndPushers();
            Place(camera);
            Draw(camera);

            Vector2 farRange = new Vector2(settings.Range * (1 - settings.Fade), settings.Range);
            _farTint?.SetFarTint(_farTintColors, camera.transform.position, farRange);
            _farTintSet = _farTint != null;
        }

        private void Place(Camera camera)
        {
            float range = settings.Range;
            // Tiles grow when the range would need more of them than a dispatch takes
            float tileSize = Mathf.Max(settings.TileSize, 2 * range / (MaxTilesSide - 2));
            int tilesSide = Mathf.CeilToInt(2 * range / tileSize) + 2;
            if (_visibleTiles == null || _visibleTiles.count < tilesSide * tilesSide)
            {
                EnsureBuffer(ref _visibleTiles, tilesSide * tilesSide, sizeof(int) * 4);
            }

            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            for (int i = 0; i < _planes.Length; i++)
            {
                _frustum[i] = new Vector4(_planes[i].normal.x, _planes[i].normal.y, _planes[i].normal.z, _planes[i].distance);
            }

            ComputeShader shader = placement;
            _terrain.Bind(shader, _cullKernel);
            _terrain.Bind(shader, _placeKernel);
            shader.SetVector(CameraProperty, camera.transform.position);
            shader.SetVectorArray(Frustum, _frustum);
            shader.SetFloat(RangeProperty, range);
            shader.SetFloat(FadeProperty, settings.Fade);
            shader.SetFloat(FullDensityDistance, settings.FullDensityDistance);
            shader.SetFloat(MaxWidening, settings.MaxWidening);
            shader.SetFloat(DetailedDistance, settings.DetailedMeshDistance);
            shader.SetFloat(MaxHeight, _maxHeight);
            shader.SetFloat(TileSize, tileSize);
            shader.SetInt(TilesSide, tilesSide);
            shader.SetFloat(Occlusion, settings.TerrainOcclusion ? 1 : 0);
            shader.SetInt(OcclusionSteps, settings.OcclusionSteps);
            shader.SetFloat(Shadows, settings.CastShadows ? 1 : 0);
            shader.SetInt(SurfaceCount, _surfaceLayers.count);
            shader.SetFloat(MaxDensity, _maxDensity);
            shader.SetInt(BatchCount, _batches.Count);
            shader.SetInt(AreaCount, _areaData.Count);
            shader.SetInt(PusherCount, _pusherData.Count);

            shader.SetBuffer(_resetKernel, DispatchArgs, _dispatchArgs);
            shader.SetBuffer(_resetKernel, DrawArgs, _drawArgs);
            shader.SetBuffer(_cullKernel, DispatchArgs, _dispatchArgs);
            shader.SetBuffer(_cullKernel, VisibleTiles, _visibleTiles);
            shader.SetBuffer(_placeKernel, VisibleTiles, _visibleTiles);
            shader.SetBuffer(_placeKernel, DrawArgs, _drawArgs);
            shader.SetBuffer(_placeKernel, Blades, _blades);
            shader.SetBuffer(_placeKernel, Batches, _batchBuffer);
            shader.SetBuffer(_placeKernel, LayersProperty, _layers);
            shader.SetBuffer(_placeKernel, SurfaceLayers, _surfaceLayers);
            shader.SetBuffer(_placeKernel, Areas, _areaBuffer);
            shader.SetBuffer(_placeKernel, Polygons, _polygonBuffer);
            shader.SetBuffer(_placeKernel, Pushers, _pusherBuffer);
            shader.SetBuffer(_clampKernel, DrawArgs, _drawArgs);
            shader.SetBuffer(_clampKernel, Batches, _batchBuffer);

            int batchGroups = Mathf.CeilToInt(_batches.Count / 64f);
            shader.Dispatch(_resetKernel, batchGroups, 1, 1);
            int groups = Mathf.CeilToInt(tilesSide / 8f);
            shader.Dispatch(_cullKernel, groups, groups, 1);
            shader.DispatchIndirect(_placeKernel, _dispatchArgs, 0);
            shader.Dispatch(_clampKernel, batchGroups, 1, 1);
        }

        private void Draw(Camera camera)
        {
            float direction = settings.WindDirection * Mathf.Deg2Rad;
            _material.SetVector(Wind, new Vector4(Mathf.Sin(direction), Mathf.Cos(direction), settings.WindStrength, settings.WindWaveLength));
            _material.SetVector(WindShape, new Vector4(settings.WindSpeed, settings.WindGustiness, settings.WindFlutter, 0));
            _material.SetVector(Look, new Vector4(settings.RootDarkening, settings.Smoothness, settings.TerrainNormal, 0));
            _material.SetBuffer(LayersProperty, _layers);
            _material.SetBuffer(Palette, _palette);

            var renderParams = new RenderParams(_material)
            {
                worldBounds = new Bounds(camera.transform.position, Vector3.one * (settings.Range * 2 + 200)),
                camera = camera,
                layer = gameObject.layer,
                receiveShadows = true,
                motionVectorMode = MotionVectorGenerationMode.Camera,
            };
            for (int i = 0; i < _batches.Count; i++)
            {
                Batch batch = _batches[i];
                renderParams.matProps = batch.Properties;
                renderParams.shadowCastingMode = batch.Near && settings.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                Graphics.RenderMeshIndirect(renderParams, batch.Mesh, _drawArgs, 1, i);
            }
        }

        private void UploadAreasAndPushers()
        {
            _areaData.Clear();
            _polygonPoints.Clear();
            _areas.RemoveAll(area => area is Object destroyed && !destroyed);
            foreach (IGrassArea area in _areas)
            {
                GrassAreaData data = area.Area;
                int polygonStart = _polygonPoints.Count;
                int polygonCount = 0;
                if (data.Shape == GrassAreaShape.Polygon)
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

            _pusherData.Clear();
            _pushers.RemoveAll(pusher => pusher is Object destroyed && !destroyed);
            foreach (IGrassPusher pusher in _pushers)
            {
                GrassPusherData data = pusher.Pusher;
                _pusherData.Add(new PusherData { Center = data.Center, Radius = data.Radius, Strength = data.Strength });
            }

            // At least one element: every buffer a kernel reads must be bound
            EnsureBuffer(ref _areaBuffer, Mathf.Max(1, _areaData.Count), AreaData.Stride);
            EnsureBuffer(ref _polygonBuffer, Mathf.Max(1, _polygonPoints.Count), sizeof(float) * 2);
            EnsureBuffer(ref _pusherBuffer, Mathf.Max(1, _pusherData.Count), PusherData.Stride);
            if (_areaData.Count > 0)
            {
                _areaBuffer.SetData(_areaData);
            }
            if (_polygonPoints.Count > 0)
            {
                _polygonBuffer.SetData(_polygonPoints);
            }
            if (_pusherData.Count > 0)
            {
                _pusherBuffer.SetData(_pusherData);
            }
        }

        // Grass layers of every surface id, the shapes they use, and the far tint of the terrain
        private void BuildSurfaces()
        {
            _surfacesDirty = false;
            _shapes.Clear();
            _shapes.Add(null);
            var surfaces = _terrain.Surfaces;
            int count = Mathf.Max(1, surfaces.Count);
            var ranges = new Vector2Int[count];
            var layers = new List<LayerData>();
            var palette = new List<uint>();
            var tint = new Vector4[count];
            _maxDensity = 0;
            _maxHeight = 0;
            for (int id = 0; id < surfaces.Count; id++)
            {
                ranges[id].x = layers.Count;
                float surfaceDensity = 0;
                float tintWeight = 0;
                Vector4 surfaceTint = Vector4.zero;
                foreach (GrassLayer layer in settings.Layers)
                {
                    if (layer == null || !surfaces[id] || layer.Surface != surfaces[id] || layer.Density <= 0 ||
                        (layer.NoiseScale > 0 && layer.NoiseCoverage <= 0))
                    {
                        continue;
                    }

                    Color root = layer.RootColor.linear;
                    Color tip = layer.TipColor.linear;
                    layers.Add(new LayerData
                    {
                        Density = layer.Density,
                        Height = layer.Height,
                        Width = layer.Width,
                        Bend = layer.Bend,
                        ColorVariation = layer.ColorVariation,
                        RootColor = new Vector3(root.r, root.g, root.b),
                        TipColor = new Vector3(tip.r, tip.g, tip.b),
                        Shape = (uint)ShapeIndex(layer.Shape),
                        Noise = NoiseOf(layer),
                        NoiseSeed = (uint)layer.NoiseSeed,
                        Part0 = PartColors(layer, 0, palette),
                        Part1 = PartColors(layer, 1, palette),
                        Part2 = PartColors(layer, 2, palette),
                        Part3 = PartColors(layer, 3, palette),
                    });
                    surfaceDensity += layer.Density;
                    _maxHeight = Mathf.Max(_maxHeight, layer.Height.y);

                    // Seen from afar a meadow shows mostly the upper part of the plants; layers mix by their plants
                    float weight = layer.Density * (layer.NoiseScale > 0 ? layer.NoiseCoverage : 1);
                    Color far = Color.Lerp(root, tip, 0.65f);
                    surfaceTint += new Vector4(far.r, far.g, far.b, layer.FarTint) * weight;
                    tintWeight += weight;
                }
                ranges[id].y = layers.Count - ranges[id].x;
                tint[id] = tintWeight > 0 ? surfaceTint / tintWeight : Vector4.zero;
                _maxDensity = Mathf.Max(_maxDensity, surfaceDensity);
            }

            // At least one element: every buffer a kernel reads must be bound
            if (layers.Count == 0)
            {
                layers.Add(default);
            }
            if (palette.Count == 0)
            {
                palette.Add(0);
            }
            EnsureBuffer(ref _layers, layers.Count, LayerData.Stride);
            EnsureBuffer(ref _palette, palette.Count, sizeof(uint));
            EnsureBuffer(ref _surfaceLayers, count, sizeof(int) * 2);
            EnsureBuffer(ref _farTintColors, count, sizeof(float) * 4);
            _layers.SetData(layers);
            _palette.SetData(palette);
            _surfaceLayers.SetData(ranges);
            _farTintColors.SetData(tint);
            SubscribeShapes();
        }

        // Own colors of a part of the shape of a layer, added to the palette: their first index (low 20 bits) and count
        // (high 12 bits). 0: the part takes the root and tip colors
        private static uint PartColors(GrassLayer layer, int part, List<uint> palette)
        {
            GrassPartColor colors = layer.Parts != null && part < layer.Parts.Count ? layer.Parts[part] : null;
            if (colors == null || !colors.OverrideColor || colors.Colors == null || colors.Colors.Count == 0)
            {
                return 0;
            }

            int first = palette.Count;
            int count = Mathf.Min(colors.Colors.Count, GrassPartColor.MaxColors);
            for (int i = 0; i < count; i++)
            {
                Color color = colors.Colors[i].linear;
                palette.Add((uint)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255) |
                            (uint)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255) << 8 |
                            (uint)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255) << 16);
            }
            return (uint)first | (uint)count << 20;
        }

        // Density patches of a layer for GrassLayerNoise: frequency, the noise values of the edges, inverted
        private static Vector4 NoiseOf(GrassLayer layer)
        {
            if (layer.NoiseScale <= 0 || layer.NoiseCoverage >= 1)
            {
                return Vector4.zero;
            }

            float position = Mathf.Clamp01(layer.NoiseCoverage) * (NoiseThresholds.Length - 1);
            int index = Mathf.Min((int)position, NoiseThresholds.Length - 2);
            float threshold = Mathf.Lerp(NoiseThresholds[index], NoiseThresholds[index + 1], position - index);
            // Softness 1 spreads an edge over about one standard deviation of the noise each way
            float halfWidth = Mathf.Max(1e-3f, layer.NoiseSoftness * 0.17f);
            return new Vector4(1 / layer.NoiseScale, threshold - halfWidth, threshold + halfWidth, layer.NoiseInvert ? 1 : 0);
        }

        // Index of a usable shape, 0 (the built-in blade) when it can't be used
        private int ShapeIndex(GrassShape shape)
        {
            if (!shape)
            {
                return 0;
            }

            int index = _shapes.IndexOf(shape);
            if (index >= 0)
            {
                return index;
            }

            if (!Usable(shape.Mesh) || !Usable(shape.FarMesh))
            {
                if (_reportedShapes.Add(shape))
                {
                    Debug.LogWarning($"Grass shape {shape.name} needs meshes with Read/Write enabled; the built-in blade is used instead", shape);
                }
                return 0;
            }

            _shapes.Add(shape);
            return _shapes.Count - 1;
        }

        private static bool Usable(Mesh mesh) => mesh && mesh.isReadable && mesh.vertexCount > 0;

        // Batch meshes and the parts of the plant buffer, rebuilt only when the shapes or their limits change
        private void BuildGeometryWhenChanged()
        {
            if (!_builtInBlade || _builtInSegments != settings.DetailedSegments)
            {
                DestroyObject(_builtInBlade);
                _builtInSegments = settings.DetailedSegments;
                _builtInBlade = GrassBladeMesh.CreateBlade(_builtInSegments, "Grass blade");
                _geometryKey = null;
            }
            if (!_builtInFarBlade)
            {
                _builtInFarBlade = GrassBladeMesh.CreateBlade(1, "Grass blade, far");
            }

            var key = new StringBuilder();
            key.Append(_builtInSegments).Append(',').Append(settings.MaxDetailedBlades).Append(',').Append(settings.MaxBlades);
            for (int s = 1; s < _shapes.Count; s++)
            {
                GrassShape shape = _shapes[s];
                key.Append('|').Append(shape.Mesh.GetInstanceID()).Append(',').Append(shape.FarMesh.GetInstanceID())
                    .Append(',').Append(shape.MaxNear).Append(',').Append(shape.MaxFar)
                    .Append(',').Append(shape.Mesh.vertexCount).Append(',').Append(shape.FarMesh.vertexCount);
            }
            if (key.ToString() == _geometryKey && _drawArgs != null)
            {
                return;
            }
            _geometryKey = key.ToString();

            foreach (Batch batch in _batches)
            {
                DestroyObject(batch.Mesh);
            }
            _batches.Clear();

            int offset = 0;
            for (int s = 0; s < _shapes.Count; s++)
            {
                GrassShape shape = _shapes[s];
                Mesh near = shape ? shape.Mesh : _builtInBlade;
                Mesh far = shape ? shape.FarMesh : _builtInFarBlade;
                AddBatch(near, shape ? shape.MaxNear : settings.MaxDetailedBlades, true, ref offset);
                AddBatch(far, shape ? shape.MaxFar : settings.MaxBlades, false, ref offset);
            }

            var batchData = new BatchData[_batches.Count];
            for (int i = 0; i < _batches.Count; i++)
            {
                batchData[i] = new BatchData
                {
                    Offset = (uint)_batches[i].Offset,
                    Capacity = (uint)_batches[i].Capacity,
                    IndicesPerPlant = (uint)_batches[i].IndicesPerPlant,
                };
            }
            EnsureBuffer(ref _batchBuffer, _batches.Count, BatchData.Stride);
            _batchBuffer.SetData(batchData);

            EnsureBuffer(ref _blades, Mathf.Max(1, offset), BladeStride);
            foreach (Batch batch in _batches)
            {
                batch.Properties.SetBuffer(Blades, _blades);
            }

            // Index counts are written by the placement; each batch is drawn once
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[_batches.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i].instanceCount = 1;
            }
            _drawArgs?.Release();
            _drawArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured,
                args.Length, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _drawArgs.SetData(args);
        }

        private void AddBatch(Mesh shape, int plants, bool near, ref int offset)
        {
            int capacity = GrassBladeMesh.FitPlants(shape, plants);
            var batch = new Batch
            {
                Mesh = GrassBladeMesh.CreateBatch(shape, capacity, $"Grass batch ({shape.name}, {(near ? "near" : "far")})"),
                Near = near,
                Offset = offset,
                Capacity = capacity,
                IndicesPerPlant = GrassBladeMesh.IndicesPerPlant(shape),
                Properties = new MaterialPropertyBlock(),
            };
            batch.Properties.SetFloat(BladeOffset, offset);
            batch.Properties.SetFloat(VerticesPerPlant, shape.vertexCount);
            _batches.Add(batch);
            offset += capacity;
        }

        private void EnsureResources()
        {
            if (!_material)
            {
                _material = new Material(bladeShader)
                {
                    name = "Grass",
                    hideFlags = HideFlags.DontSave,
                };
                // Sets up the keywords and passes of an HDRP material made from code
                HDMaterial.ValidateMaterial(_material);
                _resetKernel = placement.FindKernel("Reset");
                _cullKernel = placement.FindKernel("CullTiles");
                _placeKernel = placement.FindKernel("PlaceBlades");
                _clampKernel = placement.FindKernel("ClampArgs");
            }

            if (_dispatchArgs == null)
            {
                _dispatchArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured, 3, sizeof(uint));
            }
        }

        private static void EnsureBuffer(ref GraphicsBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count == count && buffer.stride == stride)
            {
                return;
            }

            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }

        private void Subscribe()
        {
            if (_subscribedTerrain != _terrain)
            {
                if (_subscribedTerrain != null)
                {
                    _subscribedTerrain.SurfacesChanged -= OnChanged;
                }
                _subscribedTerrain = _terrain;
                _subscribedTerrain.SurfacesChanged += OnChanged;
                _surfacesDirty = true;
            }
            if (_subscribedSettings != settings)
            {
                if (_subscribedSettings)
                {
                    _subscribedSettings.Changed -= OnChanged;
                }
                _subscribedSettings = settings;
                _subscribedSettings.Changed += OnChanged;
                _surfacesDirty = true;
            }
        }

        // Edited shapes may change their meshes or limits
        private void SubscribeShapes()
        {
            UnsubscribeShapes();
            for (int s = 1; s < _shapes.Count; s++)
            {
                _shapes[s].Changed += OnChanged;
                _subscribedShapes.Add(_shapes[s]);
            }
        }

        private void UnsubscribeShapes()
        {
            foreach (GrassShape shape in _subscribedShapes)
            {
                if (shape)
                {
                    shape.Changed -= OnChanged;
                }
            }
            _subscribedShapes.Clear();
        }

        private void Unsubscribe()
        {
            if (_subscribedTerrain != null)
            {
                _subscribedTerrain.SurfacesChanged -= OnChanged;
                _subscribedTerrain = null;
            }
            if (_subscribedSettings)
            {
                _subscribedSettings.Changed -= OnChanged;
                _subscribedSettings = null;
            }
            UnsubscribeShapes();
        }

        private void OnChanged()
        {
            _surfacesDirty = true;
            _reportedShapes.Clear();
        }

        private void ClearFarTint()
        {
            if (_farTintSet)
            {
                _farTintSet = false;
                _farTint?.ClearFarTint();
            }
        }

        private void OnDisable()
        {
            ClearFarTint();
        }

        private void OnDestroy()
        {
            ClearFarTint();
            Unsubscribe();
            _layers?.Release();
            _surfaceLayers?.Release();
            _palette?.Release();
            _farTintColors?.Release();
            _batchBuffer?.Release();
            _areaBuffer?.Release();
            _polygonBuffer?.Release();
            _pusherBuffer?.Release();
            _dispatchArgs?.Release();
            _drawArgs?.Release();
            _visibleTiles?.Release();
            _blades?.Release();
            foreach (Batch batch in _batches)
            {
                DestroyObject(batch.Mesh);
            }
            DestroyObject(_material);
            DestroyObject(_builtInBlade);
            DestroyObject(_builtInFarBlade);
        }

        private static void DestroyObject(Object target)
        {
            if (!target)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
