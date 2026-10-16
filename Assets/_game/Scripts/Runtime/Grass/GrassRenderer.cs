using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Core;
using Core.Grass;
using Core.Scatter;
using Core.TerrainGenerator;
using Runtime.Scatter;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

namespace Runtime.Grass
{
    /// <summary>
    /// Grass on the terrain, all on the GPU: a kind of scattered detail (<see cref="ScatterPipeline"/>), placed every
    /// frame around the main camera by GrassPlacement.compute, out of the areas of the scatter system and the ground
    /// covered by the kinds placed before it (stones), and bent by the pushers. Plants are drawn in batches, one per shape and distance (near, far).
    /// Past the range the terrain takes the grass color (ITerrainFarTint).
    /// </summary>
    public class GrassRenderer : MonoBehaviour, IMyInstaller, IGrass
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 16;

            public Vector2 Height;
            public float Width;
            public float Bend;
            public float ColorVariation;
            public Vector3 RootColor;
            public Vector3 TipColor;
            public uint Shape;
            public uint Part0;
            public uint Part1;
            public uint Part2;
            public uint Part3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PusherData
        {
            public const int Stride = sizeof(float) * 5;

            public Vector3 Center;
            public float Radius;
            public float Strength;
        }

        private const int BladeStride = sizeof(float) * 14;

        [SerializeField] private GrassSettings settings;
        [SerializeField] private ComputeShader placement;
        [PropertyTooltip("Grass.shadergraph")]
        [SerializeField] private Shader bladeShader;
        [SerializeField] private bool drawGrass = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private ITerrainFarTint _farTint;
        [Inject(Optional = true)] private IScatterGpuAreas _areas;
        [Inject(Optional = true)] private IScatterGpuCover _cover;

        private readonly List<IGrassPusher> _pushers = new ();
        private readonly List<PusherData> _pusherData = new ();
        private readonly List<LayerData> _layerData = new ();
        private readonly List<uint> _paletteData = new ();
        private readonly List<ScatterPipeline.BatchSource> _batchSources = new ();

        // Shape 0 is the built-in blade
        private readonly List<GrassShape> _shapes = new () { null };
        private readonly List<GrassShape> _subscribedShapes = new ();
        private readonly HashSet<GrassShape> _reportedShapes = new ();
        private readonly ScatterLayerTable _layerTable = new ();
        private ScatterPipeline _pipeline;
        private string _geometryKey;
        private ITerrainGpuData _subscribedTerrain;
        private GrassSettings _subscribedSettings;
        private bool _surfacesDirty = true;
        private Material _material;
        private Mesh _builtInBlade;
        private Mesh _builtInFarBlade;
        private int _builtInSegments;
        private GraphicsBuffer _layers;
        private GraphicsBuffer _palette;
        private GraphicsBuffer _farTintColors;
        private GraphicsBuffer _pusherBuffer;
        private float _maxHeight;
        private bool _farTintSet;

        private static readonly int LayersProperty = Shader.PropertyToID("grass_layers");
        private static readonly int Palette = Shader.PropertyToID("grass_palette");
        private static readonly int MaxWidening = Shader.PropertyToID("grass_max_widening");
        private static readonly int Pushers = Shader.PropertyToID("grass_pushers");
        private static readonly int PusherCount = Shader.PropertyToID("grass_pusher_count");
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
            if (_layerTable.MaxDensity <= 0)
            {
                ClearFarTint();
                return;
            }

            UploadPushers();
            Place(camera);
            Draw(camera);

            Vector2 farRange = new Vector2(settings.Range * (1 - settings.Fade), settings.Range);
            _farTint?.SetFarTint(_farTintColors, camera.transform.position, farRange);
            _farTintSet = _farTint != null;
        }

        private void Place(Camera camera)
        {
            ComputeShader shader = _pipeline.Shader;
            int kernel = _pipeline.PlaceKernel;
            shader.SetBuffer(kernel, LayersProperty, _layers);
            shader.SetBuffer(kernel, Pushers, _pusherBuffer);
            shader.SetInt(PusherCount, _pusherData.Count);
            shader.SetFloat(MaxWidening, settings.MaxWidening);

            var placementSettings = new ScatterPipeline.Settings
            {
                Range = settings.Range,
                Fade = settings.Fade,
                FullDensityDistance = settings.FullDensityDistance,
                DetailedDistance = settings.DetailedMeshDistance,
                MaxHeight = _maxHeight,
                TileSize = settings.TileSize,
                TerrainOcclusion = settings.TerrainOcclusion,
                OcclusionSteps = settings.OcclusionSteps,
                CastShadows = settings.CastShadows,
            };
            _pipeline.Place(camera, placementSettings, _terrain, _layerTable, _areas, _cover,
                ScatterCoverUse.ReadLarge | ScatterCoverUse.ReadSmall);
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
            _pipeline.Draw(renderParams, settings.CastShadows);
        }

        private void UploadPushers()
        {
            _pusherData.Clear();
            _pushers.RemoveAll(pusher => pusher is Object destroyed && !destroyed);
            foreach (IGrassPusher pusher in _pushers)
            {
                GrassPusherData data = pusher.Pusher;
                _pusherData.Add(new PusherData { Center = data.Center, Radius = data.Radius, Strength = data.Strength });
            }

            // At least one element: every buffer a kernel reads must be bound
            ScatterUtility.EnsureBuffer(ref _pusherBuffer, Mathf.Max(1, _pusherData.Count), PusherData.Stride);
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
            _layerData.Clear();
            _paletteData.Clear();
            _maxHeight = 0;
            var surfaces = _terrain.Surfaces;
            var tint = new Vector4[Mathf.Max(1, surfaces.Count)];
            var tintWeights = new float[tint.Length];

            _layerTable.Build(surfaces, settings.Layers, (id, layer) =>
            {
                Color root = layer.RootColor.linear;
                Color tip = layer.TipColor.linear;
                _layerData.Add(new LayerData
                {
                    Height = layer.Height,
                    Width = layer.Width,
                    Bend = layer.Bend,
                    ColorVariation = layer.ColorVariation,
                    RootColor = new Vector3(root.r, root.g, root.b),
                    TipColor = new Vector3(tip.r, tip.g, tip.b),
                    Shape = (uint)ShapeIndex(layer.Shape),
                    Part0 = PartColors(layer, 0, _paletteData),
                    Part1 = PartColors(layer, 1, _paletteData),
                    Part2 = PartColors(layer, 2, _paletteData),
                    Part3 = PartColors(layer, 3, _paletteData),
                });
                _maxHeight = Mathf.Max(_maxHeight, layer.Height.y);

                // Seen from afar a meadow shows mostly the upper part of the plants; layers mix by their plants
                float weight = layer.Density * (layer.NoiseScale > 0 ? layer.NoiseCoverage : 1);
                Color far = Color.Lerp(root, tip, 0.65f);
                tint[id] += new Vector4(far.r, far.g, far.b, layer.FarTint) * weight;
                tintWeights[id] += weight;
            });
            for (int id = 0; id < tint.Length; id++)
            {
                tint[id] = tintWeights[id] > 0 ? tint[id] / tintWeights[id] : Vector4.zero;
            }

            // At least one element: every buffer a kernel reads must be bound
            if (_layerData.Count == 0)
            {
                _layerData.Add(default);
            }
            if (_paletteData.Count == 0)
            {
                _paletteData.Add(0);
            }
            ScatterUtility.EnsureBuffer(ref _layers, _layerData.Count, LayerData.Stride);
            ScatterUtility.EnsureBuffer(ref _palette, _paletteData.Count, sizeof(uint));
            ScatterUtility.EnsureBuffer(ref _farTintColors, tint.Length, sizeof(float) * 4);
            _layers.SetData(_layerData);
            _palette.SetData(_paletteData);
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
                ScatterUtility.DestroyObject(_builtInBlade);
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
            if (key.ToString() == _geometryKey && _pipeline.Batches.Count > 0)
            {
                return;
            }
            _geometryKey = key.ToString();

            // Shape s: near batch s * 2, far batch s * 2 + 1
            _batchSources.Clear();
            for (int s = 0; s < _shapes.Count; s++)
            {
                GrassShape shape = _shapes[s];
                _batchSources.Add(new ScatterPipeline.BatchSource
                {
                    Shape = shape ? shape.Mesh : _builtInBlade,
                    Capacity = shape ? shape.MaxNear : settings.MaxDetailedBlades,
                    Near = true,
                });
                _batchSources.Add(new ScatterPipeline.BatchSource
                {
                    Shape = shape ? shape.FarMesh : _builtInFarBlade,
                    Capacity = shape ? shape.MaxFar : settings.MaxBlades,
                    Near = false,
                });
            }
            _pipeline.SetBatches(_batchSources, "Grass");
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
            }

            if (_pipeline == null || _pipeline.Shader != placement)
            {
                _pipeline?.Dispose();
                _pipeline = new ScatterPipeline(placement, BladeStride);
                _geometryKey = null;
                _surfacesDirty = true;
            }
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
            _pipeline?.Dispose();
            _layerTable.Dispose();
            _layers?.Release();
            _palette?.Release();
            _farTintColors?.Release();
            _pusherBuffer?.Release();
            ScatterUtility.DestroyObject(_material);
            ScatterUtility.DestroyObject(_builtInBlade);
            ScatterUtility.DestroyObject(_builtInFarBlade);
        }
    }
}
