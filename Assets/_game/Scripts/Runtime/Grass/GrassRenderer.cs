using System.Runtime.InteropServices;
using Core;
using Core.Grass;
using Core.TerrainGenerator;
using Core.TerrainGenerator.Surfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

namespace Runtime.Grass
{
    /// <summary>
    /// Grass on the terrain, all on the GPU. Every frame GrassPlacement.compute culls tiles around the main camera
    /// (range, frustum, hills in the way) and places blades on the terrain mesh by the surface types under them; the
    /// blades are drawn with Graphics.RenderMeshIndirect, a curved mesh near the camera and a triangle farther. The
    /// meshes hold a blade for every slot of the blade buffers (GrassBladeMesh) and the placement sets how many are drawn.
    /// Past the range the terrain takes the grass color (ITerrainFarTint).
    /// </summary>
    public class GrassRenderer : MonoBehaviour, IMyInstaller, IGrass
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct GrassSurfaceData
        {
            public const int Stride = sizeof(float) * 12;

            public float Density;
            public Vector2 Height;
            public float Width;
            public float Bend;
            public float ColorVariation;
            public Vector3 RootColor;
            public Vector3 TipColor;
        }

        private const int BladeStride = sizeof(float) * 12;
        // Thread groups of a dispatch dimension are limited to 65535: one per visible tile
        private const int MaxTilesSide = 240;

        [SerializeField] private GrassSettings settings;
        [SerializeField] private ComputeShader placement;
        [Tooltip("Grass.shadergraph")]
        [SerializeField] private Shader bladeShader;
        [SerializeField] private bool drawGrass = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private ITerrainFarTint _farTint;

        private ITerrainGpuData _subscribedTerrain;
        private GrassSettings _subscribedSettings;
        private bool _surfacesDirty = true;
        private Material _material;
        private MaterialPropertyBlock _detailedProperties;
        private MaterialPropertyBlock _simpleProperties;
        private Mesh _detailedMesh;
        private Mesh _simpleMesh;
        private int _detailedSegments;
        private int _maxDetailedBlades;
        private GraphicsBuffer _surfaces;
        private GraphicsBuffer _farTintColors;
        private GraphicsBuffer _dispatchArgs;
        private GraphicsBuffer _drawArgs;
        private GraphicsBuffer _visibleTiles;
        private GraphicsBuffer _bladesDetailed;
        private GraphicsBuffer _bladesSimple;
        private int _maxBlades;
        private float _maxDensity;
        private float _maxHeight;
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustum = new Vector4[6];
        private int _resetKernel;
        private int _cullKernel;
        private int _placeKernel;
        private int _clampKernel;
        private bool _farTintSet;

        private static readonly int SurfacesProperty = Shader.PropertyToID("grass_surfaces");
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
        private static readonly int MaxBlades = Shader.PropertyToID("grass_max_blades");
        private static readonly int IndicesPerBlade = Shader.PropertyToID("grass_indices_per_blade");
        private static readonly int DispatchArgs = Shader.PropertyToID("grass_dispatch_args");
        private static readonly int DrawArgs = Shader.PropertyToID("grass_draw_args");
        private static readonly int VisibleTiles = Shader.PropertyToID("grass_visible_tiles");
        private static readonly int BladesDetailed = Shader.PropertyToID("grass_blades_detailed");
        private static readonly int BladesSimple = Shader.PropertyToID("grass_blades_simple");
        private static readonly int Blades = Shader.PropertyToID("grass_blades");
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
            }
            if (_maxDensity <= 0)
            {
                ClearFarTint();
                return;
            }

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
            EnsureVisibleTiles(tilesSide * tilesSide);

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
            shader.SetInts(MaxBlades, _maxDetailedBlades, _maxBlades);
            shader.SetInts(IndicesPerBlade, GrassBladeMesh.IndicesPerBlade(_detailedSegments), GrassBladeMesh.IndicesPerBlade(1));
            shader.SetInt(SurfaceCount, _surfaces.count);
            shader.SetFloat(MaxDensity, _maxDensity);

            shader.SetBuffer(_resetKernel, DispatchArgs, _dispatchArgs);
            shader.SetBuffer(_resetKernel, DrawArgs, _drawArgs);
            shader.SetBuffer(_cullKernel, DispatchArgs, _dispatchArgs);
            shader.SetBuffer(_cullKernel, VisibleTiles, _visibleTiles);
            shader.SetBuffer(_placeKernel, VisibleTiles, _visibleTiles);
            shader.SetBuffer(_placeKernel, DrawArgs, _drawArgs);
            shader.SetBuffer(_placeKernel, BladesDetailed, _bladesDetailed);
            shader.SetBuffer(_placeKernel, BladesSimple, _bladesSimple);
            shader.SetBuffer(_placeKernel, SurfacesProperty, _surfaces);
            shader.SetBuffer(_clampKernel, DrawArgs, _drawArgs);

            shader.Dispatch(_resetKernel, 1, 1, 1);
            int groups = Mathf.CeilToInt(tilesSide / 8f);
            shader.Dispatch(_cullKernel, groups, groups, 1);
            shader.DispatchIndirect(_placeKernel, _dispatchArgs, 0);
            shader.Dispatch(_clampKernel, 1, 1, 1);
        }

        private void Draw(Camera camera)
        {
            float direction = settings.WindDirection * Mathf.Deg2Rad;
            _material.SetVector(Wind, new Vector4(Mathf.Sin(direction), Mathf.Cos(direction), settings.WindStrength, settings.WindWaveLength));
            _material.SetVector(WindShape, new Vector4(settings.WindSpeed, settings.WindGustiness, settings.WindFlutter, 0));
            _material.SetVector(Look, new Vector4(settings.RootDarkening, settings.Smoothness, settings.TerrainNormal, 0));

            var renderParams = new RenderParams(_material)
            {
                worldBounds = new Bounds(camera.transform.position, Vector3.one * (settings.Range * 2 + 200)),
                camera = camera,
                layer = gameObject.layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                motionVectorMode = MotionVectorGenerationMode.Camera,
                matProps = _detailedProperties,
            };
            Graphics.RenderMeshIndirect(renderParams, _detailedMesh, _drawArgs, 1, 0);
            renderParams.matProps = _simpleProperties;
            Graphics.RenderMeshIndirect(renderParams, _simpleMesh, _drawArgs, 1, 1);
        }

        // Grass of every surface id, and the far tint of the terrain
        private void BuildSurfaces()
        {
            _surfacesDirty = false;
            var surfaces = _terrain.Surfaces;
            int count = Mathf.Max(1, surfaces.Count);
            var data = new GrassSurfaceData[count];
            var tint = new Vector4[count];
            _maxDensity = 0;
            _maxHeight = 0;
            for (int id = 0; id < surfaces.Count; id++)
            {
                GrassLayer layer = FindLayer(surfaces[id]);
                if (layer == null || layer.Density <= 0)
                {
                    continue;
                }

                Color root = layer.RootColor.linear;
                Color tip = layer.TipColor.linear;
                data[id] = new GrassSurfaceData
                {
                    Density = layer.Density,
                    Height = layer.Height,
                    Width = layer.Width,
                    Bend = layer.Bend,
                    ColorVariation = layer.ColorVariation,
                    RootColor = new Vector3(root.r, root.g, root.b),
                    TipColor = new Vector3(tip.r, tip.g, tip.b),
                };
                // Seen from afar a meadow shows mostly the upper part of the blades
                Color far = Color.Lerp(root, tip, 0.65f);
                tint[id] = new Vector4(far.r, far.g, far.b, layer.FarTint);
                _maxDensity = Mathf.Max(_maxDensity, layer.Density);
                _maxHeight = Mathf.Max(_maxHeight, layer.Height.y);
            }

            EnsureBuffer(ref _surfaces, count, GrassSurfaceData.Stride, GraphicsBuffer.Target.Structured);
            EnsureBuffer(ref _farTintColors, count, sizeof(float) * 4, GraphicsBuffer.Target.Structured);
            _surfaces.SetData(data);
            _farTintColors.SetData(tint);
        }

        private GrassLayer FindLayer(SurfaceType surface)
        {
            if (!surface)
            {
                return null;
            }

            foreach (GrassLayer layer in settings.Layers)
            {
                if (layer != null && layer.Surface == surface)
                {
                    return layer;
                }
            }
            return null;
        }

        private void EnsureResources()
        {
            if (!_material)
            {
                _material = new Material(bladeShader)
                {
                    name = "Grass blades",
                    hideFlags = HideFlags.DontSave,
                };
                // Sets up the keywords and passes of an HDRP material made from code
                HDMaterial.ValidateMaterial(_material);
                _detailedProperties = new MaterialPropertyBlock();
                _simpleProperties = new MaterialPropertyBlock();
                _resetKernel = placement.FindKernel("Reset");
                _cullKernel = placement.FindKernel("CullTiles");
                _placeKernel = placement.FindKernel("PlaceBlades");
                _clampKernel = placement.FindKernel("ClampArgs");
            }

            // The meshes hold a blade for every slot of the blade buffers
            if (!_detailedMesh || _detailedSegments != settings.DetailedSegments || _maxDetailedBlades != settings.MaxDetailedBlades)
            {
                DestroyObject(_detailedMesh);
                _detailedSegments = settings.DetailedSegments;
                _maxDetailedBlades = settings.MaxDetailedBlades;
                _detailedMesh = GrassBladeMesh.Create(_detailedSegments, _maxDetailedBlades, "Grass blades");
                EnsureBuffer(ref _bladesDetailed, _maxDetailedBlades, BladeStride, GraphicsBuffer.Target.Structured);
                _detailedProperties.SetBuffer(Blades, _bladesDetailed);
            }
            if (!_simpleMesh || _maxBlades != settings.MaxBlades)
            {
                DestroyObject(_simpleMesh);
                _maxBlades = settings.MaxBlades;
                _simpleMesh = GrassBladeMesh.Create(1, _maxBlades, "Grass blades, far");
                EnsureBuffer(ref _bladesSimple, _maxBlades, BladeStride, GraphicsBuffer.Target.Structured);
                _simpleProperties.SetBuffer(Blades, _bladesSimple);
            }

            if (_dispatchArgs == null)
            {
                _dispatchArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured, 3, sizeof(uint));
            }
            if (_drawArgs == null)
            {
                // Index counts are written by the placement; each mesh is drawn once
                _drawArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured, 2,
                    GraphicsBuffer.IndirectDrawIndexedArgs.size);
                _drawArgs.SetData(new[]
                {
                    new GraphicsBuffer.IndirectDrawIndexedArgs { instanceCount = 1 },
                    new GraphicsBuffer.IndirectDrawIndexedArgs { instanceCount = 1 },
                });
            }
        }

        private void EnsureVisibleTiles(int count)
        {
            if (_visibleTiles == null || _visibleTiles.count < count)
            {
                EnsureBuffer(ref _visibleTiles, count, sizeof(int) * 4, GraphicsBuffer.Target.Structured);
            }
        }

        private static void EnsureBuffer(ref GraphicsBuffer buffer, int count, int stride, GraphicsBuffer.Target target)
        {
            if (buffer != null && buffer.count == count && buffer.stride == stride)
            {
                return;
            }

            buffer?.Release();
            buffer = new GraphicsBuffer(target, count, stride);
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
        }

        private void OnChanged()
        {
            _surfacesDirty = true;
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
            _surfaces?.Release();
            _farTintColors?.Release();
            _dispatchArgs?.Release();
            _drawArgs?.Release();
            _visibleTiles?.Release();
            _bladesDetailed?.Release();
            _bladesSimple?.Release();
            DestroyObject(_material);
            DestroyObject(_detailedMesh);
            DestroyObject(_simpleMesh);
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
