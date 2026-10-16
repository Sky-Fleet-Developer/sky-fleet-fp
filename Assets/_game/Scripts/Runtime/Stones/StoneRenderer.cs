using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Core;
using Core.Scatter;
using Core.Stones;
using Core.TerrainGenerator;
using Runtime.Scatter;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

namespace Runtime.Stones
{
    /// <summary>
    /// Small stones on the terrain, all on the GPU: a kind of scattered detail (<see cref="ScatterPipeline"/>), placed
    /// every frame around the main camera by StonePlacement.compute, out of the areas of the scatter system. Stones are
    /// drawn in batches, one per shape and distance (near, far), with tiling texture arrays (StoneTextureSet) at an even
    /// texel density.
    /// </summary>
    // Placed before the grass, which skips the ground the stones cover (IScatterGpuCover)
    [DefaultExecutionOrder(-100)]
    public class StoneRenderer : MonoBehaviour, IMyInstaller, IStones
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 18;

            public Vector2 Size;
            public Vector2 SlopeAlignment;
            public Vector3 MaxRotation;
            public Vector2 Sink;
            public uint ShapeStart;
            public uint ShapeCount;
            public uint TextureStart;
            public uint TextureCount;
            public Vector3 Tint;
            public float ColorVariation;
            public float Cover;
        }

        private const int InstanceStride = sizeof(float) * 10;

        [SerializeField] private StoneSettings settings;
        [SerializeField] private ComputeShader placement;
        [PropertyTooltip("Stone.shadergraph")]
        [SerializeField] private Shader stoneShader;
        [SerializeField] private bool drawStones = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private IScatterGpuAreas _areas;
        [Inject(Optional = true)] private IScatterGpuCover _cover;

        private readonly List<StoneShape> _shapes = new ();
        private readonly List<StoneShape> _subscribedShapes = new ();
        private readonly HashSet<StoneShape> _reportedShapes = new ();
        private readonly List<LayerData> _layerData = new ();
        private readonly List<uint> _layerShapeData = new ();
        private readonly List<uint> _layerTextureData = new ();
        private readonly List<ScatterPipeline.BatchSource> _batchSources = new ();
        private readonly ScatterLayerTable _layerTable = new ();
        private ScatterPipeline _pipeline;
        private string _geometryKey;
        private ITerrainGpuData _subscribedTerrain;
        private StoneSettings _subscribedSettings;
        private StoneTextureSet _subscribedTextures;
        private bool _surfacesDirty = true;
        private Material _material;
        private GraphicsBuffer _layers;
        private GraphicsBuffer _layerShapes;
        private GraphicsBuffer _layerTextures;
        private GraphicsBuffer _shapeNormalize;
        private Texture2DArray _fallbackAlbedo;
        private Texture2DArray _fallbackNormal;
        private Texture2DArray _fallbackMask;
        private float _maxSize;

        private static readonly int LayersProperty = Shader.PropertyToID("stone_layers");
        private static readonly int LayerShapesProperty = Shader.PropertyToID("stone_layer_shapes");
        private static readonly int LayerTexturesProperty = Shader.PropertyToID("stone_layer_textures");
        private static readonly int ShapeNormalizeProperty = Shader.PropertyToID("stone_shape_normalize");
        private static readonly int TextureCountProperty = Shader.PropertyToID("stone_texture_count");
        private static readonly int UVMetersProperty = Shader.PropertyToID("stone_uv_meters");
        private static readonly int AlbedoProperty = Shader.PropertyToID("stone_albedo");
        private static readonly int NormalProperty = Shader.PropertyToID("stone_normal");
        private static readonly int MaskProperty = Shader.PropertyToID("stone_mask");
        private static readonly int UVDensityProperty = Shader.PropertyToID("stone_uv_density");
        private static readonly int StochasticProperty = Shader.PropertyToID("stone_stochastic");
        private static readonly int LookProperty = Shader.PropertyToID("stone_look");
        private static readonly int OcclusionProperty = Shader.PropertyToID("stone_occlusion");
        private static readonly int ShapeTopProperty = Shader.PropertyToID("stone_shape_top");

        public bool Enabled
        {
            get => drawStones;
            set => drawStones = value;
        }

        public StoneSettings Settings => settings;

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IStones>().FromInstance(this);
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (!drawStones || !settings || !placement || !stoneShader || _terrain == null || !_terrain.IsReady || !camera)
            {
                return;
            }

            Subscribe();
            EnsureResources();
            if (_surfacesDirty)
            {
                BuildSurfaces();
                BuildGeometryWhenChanged();
            }
            if (_layerTable.MaxDensity <= 0 || _pipeline.Batches.Count == 0)
            {
                return;
            }

            Place(camera);
            Draw(camera);
        }

        private void Place(Camera camera)
        {
            ComputeShader shader = _pipeline.Shader;
            int kernel = _pipeline.PlaceKernel;
            shader.SetBuffer(kernel, LayersProperty, _layers);
            shader.SetBuffer(kernel, LayerShapesProperty, _layerShapes);
            shader.SetBuffer(kernel, LayerTexturesProperty, _layerTextures);
            shader.SetBuffer(kernel, ShapeNormalizeProperty, _shapeNormalize);
            shader.SetInt(TextureCountProperty, TextureCount);

            var placementSettings = new ScatterPipeline.Settings
            {
                Range = settings.Range,
                Fade = settings.Fade,
                FullDensityDistance = settings.FullDensityDistance,
                DetailedDistance = settings.DetailedMeshDistance,
                MaxHeight = _maxSize,
                TileSize = settings.TileSize,
                TerrainOcclusion = settings.TerrainOcclusion,
                OcclusionSteps = settings.OcclusionSteps,
                CastShadows = settings.CastShadows,
            };
            _pipeline.Place(camera, placementSettings, _terrain, _layerTable, _areas, _cover,
                ScatterCoverUse.ReadLarge | ScatterCoverUse.WriteSmall);
        }

        private void Draw(Camera camera)
        {
            StoneTextureSet textures = settings.Textures;
            bool built = textures && textures.Count > 0;
            _material.SetTexture(AlbedoProperty, built ? textures.AlbedoArray : _fallbackAlbedo);
            _material.SetTexture(NormalProperty, built ? textures.NormalArray : _fallbackNormal);
            _material.SetTexture(MaskProperty, built ? textures.MaskArray : _fallbackMask);
            _material.SetFloat(UVDensityProperty, settings.TexelDensity / (built ? textures.Resolution : 512));
            _material.SetVector(StochasticProperty, new Vector4(settings.StochasticTiling ? 1 : 0,
                settings.StochasticContrast, settings.StochasticRotation, 0));
            _material.SetVector(LookProperty, new Vector4(settings.Smoothness, settings.NormalStrength, 0, 0));
            _material.SetVector(OcclusionProperty, settings.VertexOcclusion
                ? new Vector4(settings.VertexColorOcclusion, settings.GroundOcclusion, settings.GroundOcclusionHeight,
                    settings.OcclusionOnColor)
                : Vector4.zero);

            var renderParams = new RenderParams(_material)
            {
                worldBounds = new Bounds(camera.transform.position, Vector3.one * (settings.Range * 2 + 200)),
                camera = camera,
                layer = gameObject.layer,
                receiveShadows = true,
                motionVectorMode = MotionVectorGenerationMode.Camera,
            };
            _pipeline.Draw(renderParams, settings.CastShadows, settings.CastShadows && settings.FarShadows);
        }

        private int TextureCount => settings.Textures ? settings.Textures.Count : 0;

        // Stone layers of every surface id and the shapes and textures they use
        private void BuildSurfaces()
        {
            _surfacesDirty = false;
            _shapes.Clear();
            _layerData.Clear();
            _layerShapeData.Clear();
            _layerTextureData.Clear();
            _maxSize = 0;
            int textureCount = TextureCount;

            _layerTable.Build(_terrain.Surfaces, settings.Layers, (_, layer) =>
            {
                int shapeStart = _layerShapeData.Count;
                foreach (StoneShape shape in layer.Shapes)
                {
                    int index = ShapeIndex(shape);
                    if (index >= 0)
                    {
                        _layerShapeData.Add((uint)index);
                    }
                }

                int textureStart = _layerTextureData.Count;
                foreach (int texture in layer.Textures)
                {
                    if (texture >= 0 && texture < textureCount)
                    {
                        _layerTextureData.Add((uint)texture);
                    }
                }

                Color tint = layer.Tint.linear;
                _layerData.Add(new LayerData
                {
                    Size = layer.Size,
                    SlopeAlignment = layer.SlopeAlignment,
                    MaxRotation = layer.MaxRotation * Mathf.Deg2Rad,
                    Sink = layer.Sink,
                    ShapeStart = (uint)shapeStart,
                    ShapeCount = (uint)(_layerShapeData.Count - shapeStart),
                    TextureStart = (uint)textureStart,
                    TextureCount = (uint)(_layerTextureData.Count - textureStart),
                    Tint = new Vector3(tint.r, tint.g, tint.b),
                    ColorVariation = layer.ColorVariation,
                    Cover = layer.GrassCover,
                });
                _maxSize = Mathf.Max(_maxSize, layer.Size.y);
            }, HasUsableShape);

            // At least one element: every buffer a kernel reads must be bound
            if (_layerData.Count == 0)
            {
                _layerData.Add(default);
            }
            if (_layerShapeData.Count == 0)
            {
                _layerShapeData.Add(0);
            }
            if (_layerTextureData.Count == 0)
            {
                _layerTextureData.Add(0);
            }
            ScatterUtility.EnsureBuffer(ref _layers, _layerData.Count, LayerData.Stride);
            ScatterUtility.EnsureBuffer(ref _layerShapes, _layerShapeData.Count, sizeof(uint));
            ScatterUtility.EnsureBuffer(ref _layerTextures, _layerTextureData.Count, sizeof(uint));
            _layers.SetData(_layerData);
            _layerShapes.SetData(_layerShapeData);
            _layerTextures.SetData(_layerTextureData);
            SubscribeShapes();
        }

        private bool HasUsableShape(StoneLayer layer)
        {
            foreach (StoneShape shape in layer.Shapes)
            {
                if (IsUsable(shape))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsUsable(StoneShape shape)
        {
            if (!shape)
            {
                return false;
            }
            if (Usable(shape.Mesh) && Usable(shape.FarMesh))
            {
                return true;
            }
            if (_reportedShapes.Add(shape))
            {
                Debug.LogWarning($"Stone shape {shape.name} needs meshes with Read/Write enabled; it is skipped", shape);
            }
            return false;
        }

        // Index of a usable shape, -1 when it can't be used
        private int ShapeIndex(StoneShape shape)
        {
            if (!IsUsable(shape))
            {
                return -1;
            }

            int index = _shapes.IndexOf(shape);
            if (index >= 0)
            {
                return index;
            }
            _shapes.Add(shape);
            return _shapes.Count - 1;
        }

        private static bool Usable(Mesh mesh) => mesh && mesh.isReadable && mesh.vertexCount > 0;

        // Batch meshes and the parts of the stone buffer, rebuilt only when the shapes or their limits change
        private void BuildGeometryWhenChanged()
        {
            var key = new StringBuilder();
            foreach (StoneShape shape in _shapes)
            {
                key.Append('|').Append(shape.Mesh.GetInstanceID()).Append(',').Append(shape.FarMesh.GetInstanceID())
                    .Append(',').Append(shape.MaxNear).Append(',').Append(shape.MaxFar)
                    .Append(',').Append(shape.Mesh.vertexCount).Append(',').Append(shape.FarMesh.vertexCount);
            }
            if (key.ToString() == _geometryKey)
            {
                return;
            }
            _geometryKey = key.ToString();

            // Shape s: near batch s * 2, far batch s * 2 + 1
            _batchSources.Clear();
            var normalize = new float[Mathf.Max(1, _shapes.Count)];
            for (int s = 0; s < _shapes.Count; s++)
            {
                StoneShape shape = _shapes[s];
                _batchSources.Add(new ScatterPipeline.BatchSource { Shape = shape.Mesh, Capacity = shape.MaxNear, Near = true });
                _batchSources.Add(new ScatterPipeline.BatchSource { Shape = shape.FarMesh, Capacity = shape.MaxFar, Near = false });
                Vector3 bounds = shape.Mesh.bounds.size;
                normalize[s] = 1 / Mathf.Max(bounds.x, bounds.y, bounds.z, 1e-4f);
            }
            _pipeline.SetBatches(_batchSources, "Stone");
            for (int b = 0; b < _batchSources.Count; b++)
            {
                Mesh shape = _batchSources[b].Shape;
                _pipeline.Batches[b].Properties.SetFloat(UVMetersProperty, UVMeters(shape));
                _pipeline.Batches[b].Properties.SetFloat(ShapeTopProperty, Mathf.Max(shape.bounds.max.y, 1e-4f));
            }

            ScatterUtility.EnsureBuffer(ref _shapeNormalize, normalize.Length, sizeof(float));
            _shapeNormalize.SetData(normalize);
        }

        // Shape units per uv unit: the square root of the surface area over the uv area
        private static float UVMeters(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            if (uvs.Length != vertices.Length)
            {
                return 1;
            }

            double area = 0;
            double uvArea = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles)
                {
                    continue;
                }
                int[] indices = mesh.GetIndices(s);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    int a = indices[i];
                    int b = indices[i + 1];
                    int c = indices[i + 2];
                    area += Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).magnitude * 0.5;
                    Vector2 uvB = uvs[b] - uvs[a];
                    Vector2 uvC = uvs[c] - uvs[a];
                    uvArea += System.Math.Abs(uvB.x * uvC.y - uvB.y * uvC.x) * 0.5;
                }
            }
            return uvArea > 1e-12 ? (float)System.Math.Sqrt(area / uvArea) : 1;
        }

        private void EnsureResources()
        {
            if (!_material)
            {
                _material = new Material(stoneShader)
                {
                    name = "Stones",
                    hideFlags = HideFlags.DontSave,
                };
                // Sets up the keywords and passes of an HDRP material made from code
                HDMaterial.ValidateMaterial(_material);
            }

            if (_pipeline == null || _pipeline.Shader != placement)
            {
                _pipeline?.Dispose();
                _pipeline = new ScatterPipeline(placement, InstanceStride, true);
                _geometryKey = null;
                _surfacesDirty = true;
            }

            if (!_fallbackAlbedo)
            {
                _fallbackAlbedo = SolidArray(new Color32(128, 128, 128, 255), false, "Stone albedo fallback");
                _fallbackNormal = SolidArray(new Color32(128, 128, 0, 255), true, "Stone normal fallback");
                _fallbackMask = SolidArray(new Color32(0, 255, 128, 77), true, "Stone mask fallback");
            }
        }

        private static Texture2DArray SolidArray(Color32 color, bool linear, string name)
        {
            var array = new Texture2DArray(1, 1, 1, TextureFormat.RGBA32, false, linear)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
            };
            array.SetPixels32(new[] { color }, 0);
            array.Apply(false, true);
            return array;
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
            // Building the textures changes their count, which the layers check
            if (_subscribedTextures != settings.Textures)
            {
                if (_subscribedTextures)
                {
                    _subscribedTextures.Changed -= OnChanged;
                }
                _subscribedTextures = settings.Textures;
                if (_subscribedTextures)
                {
                    _subscribedTextures.Changed += OnChanged;
                }
                _surfacesDirty = true;
            }
        }

        // Edited shapes may change their meshes or limits
        private void SubscribeShapes()
        {
            UnsubscribeShapes();
            foreach (StoneShape shape in _shapes)
            {
                shape.Changed += OnChanged;
                _subscribedShapes.Add(shape);
            }
        }

        private void UnsubscribeShapes()
        {
            foreach (StoneShape shape in _subscribedShapes)
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
            if (_subscribedTextures)
            {
                _subscribedTextures.Changed -= OnChanged;
                _subscribedTextures = null;
            }
            UnsubscribeShapes();
        }

        private void OnChanged()
        {
            _surfacesDirty = true;
            _reportedShapes.Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _pipeline?.Dispose();
            _layerTable.Dispose();
            _layers?.Release();
            _layerShapes?.Release();
            _layerTextures?.Release();
            _shapeNormalize?.Release();
            ScatterUtility.DestroyObject(_material);
            ScatterUtility.DestroyObject(_fallbackAlbedo);
            ScatterUtility.DestroyObject(_fallbackNormal);
            ScatterUtility.DestroyObject(_fallbackMask);
        }
    }
}
