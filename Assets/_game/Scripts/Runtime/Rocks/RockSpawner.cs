using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core;
using Core.Rocks;
using Core.Scatter;
using Core.TerrainGenerator;
using Runtime.Scatter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Runtime.Rocks
{
    /// <summary>
    /// Large rocks around the camera. The terrain is split into tiles; a tile coming into range has its rocks placed on
    /// the GPU (RockPlacement.compute: the same candidates, layers, density patches and areas as the scattered
    /// details), read back, and spawned as pooled prefab instances. Rocks live in terrain space under a root following
    /// ITerrainGpuData.Origin, so they stay put when the world offset moves. When the heights are baked again (chunks
    /// loaded, generation edited) the tiles are placed again and only the rocks that changed are moved or replaced.
    /// </summary>
    public class RockSpawner : MonoBehaviour, IMyInstaller, IRocks
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 14;

            public Vector2 Scale;
            public Vector2 SlopeCos;
            public Vector2 SlopeAlignment;
            public Vector3 MaxRotation;
            public Vector2 Sink;
            public uint PrefabStart;
            public uint PrefabCount;
            public float Cover;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RockInstance
        {
            public const int Stride = sizeof(float) * 11;

            public Vector3 Position;
            public float Scale;
            public Vector4 Rotation;
            public uint Prefab;
            public uint Id;
            public float Cover;
        }

        private struct Rock
        {
            public uint Id;
            public int Prefab;
            public GameObject Instance;
            /// <summary>Terrain space x, z and the radius of the ground it covers.</summary>
            public Vector4 Cover;
        }

        private class Tile
        {
            public Vector2Int Coord;
            /// <summary>Heights version its rocks were placed for; -1 none yet.</summary>
            public int PlacedVersion = -1;
            /// <summary>Heights version its terrain wasn't loaded at; asked again at the next one.</summary>
            public int NotLoadedVersion = -1;
            public bool Requested;
            public readonly List<Rock> Rocks = new ();
        }

        private class TileResult
        {
            public Tile Tile;
            public int Version;
            public readonly List<RockInstance> Rocks = new ();
        }

        private const uint NotLoaded = 0xFFFFFFFF;
        // Moves smaller than these are not applied, so placing a tile again doesn't touch rocks that stay
        private const float PositionTolerance = 1e-3f;
        private const float RotationTolerance = 1e-4f;

        [SerializeField] private RockSettings settings;
        [SerializeField] private ComputeShader placement;
        [SerializeField] private bool placeRocks = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private IScatterGpuAreas _areas;
        [Inject(Optional = true)] private IScatterGpuCover _cover;

        private readonly Dictionary<Vector2Int, Tile> _tiles = new ();
        private readonly List<Tile> _candidates = new ();
        private readonly List<Vector2Int> _released = new ();
        private readonly Queue<TileResult> _results = new ();
        private readonly List<GameObject> _prefabs = new ();
        private readonly Dictionary<int, Stack<GameObject>> _pools = new ();
        private readonly List<LayerData> _layerData = new ();
        private readonly List<uint> _layerPrefabData = new ();
        private readonly List<float> _prefabHalfWidths = new ();
        private readonly List<Vector4> _coverData = new ();
        private readonly Dictionary<uint, Rock> _oldRocks = new ();
        private readonly ScatterLayerTable _layerTable = new ();
        private Transform _root;
        private int _kernel = -1;
        private int _coverKernel = -1;
        private bool _coverDirty = true;
        private int _heightsVersion;
        private bool _layersDirty = true;
        private ITerrainGpuData _subscribedTerrain;
        private RockSettings _subscribedSettings;
        private int _count;

        // One request at a time, its buffers kept until it is read back
        private Tile[] _requestTiles;
        private int _requestVersion;
        private int _requestId;
        private int _requestTileCount;
        private bool _requestInFlight;
        private NativeArray<RockInstance> _readInstances;
        private NativeArray<uint> _readCounts;
        private bool _instancesRead;
        private bool _countsRead;

        private GraphicsBuffer _layers;
        private GraphicsBuffer _layerPrefabs;
        private GraphicsBuffer _prefabHalfWidthBuffer;
        private GraphicsBuffer _coverBuffer;
        private GraphicsBuffer _tileBuffer;
        private GraphicsBuffer _instanceBuffer;
        private GraphicsBuffer _countBuffer;
        private GraphicsBuffer _emptyAreas;
        private GraphicsBuffer _emptyPolygons;
        private GraphicsBuffer _emptyCover;

        private static readonly int LayersProperty = Shader.PropertyToID("rock_layers");
        private static readonly int LayerPrefabsProperty = Shader.PropertyToID("rock_layer_prefabs");
        private static readonly int TilesProperty = Shader.PropertyToID("rock_tiles");
        private static readonly int MaxPerTileProperty = Shader.PropertyToID("rock_max_per_tile");
        private static readonly int InstancesProperty = Shader.PropertyToID("rock_instances");
        private static readonly int CountsProperty = Shader.PropertyToID("rock_counts");
        private static readonly int PrefabHalfWidthProperty = Shader.PropertyToID("rock_prefab_half_width");
        private static readonly int CoverProperty = Shader.PropertyToID("rock_cover");
        private static readonly int CoverCountProperty = Shader.PropertyToID("rock_cover_count");
        private static readonly int OriginProperty = Shader.PropertyToID("rock_origin");
        private static readonly int CameraProperty = Shader.PropertyToID("scatter_camera");
        private static readonly int RangeProperty = Shader.PropertyToID("scatter_range");
        private static readonly int FadeProperty = Shader.PropertyToID("scatter_fade");
        private static readonly int FullDensityDistanceProperty = Shader.PropertyToID("scatter_full_density_distance");
        private static readonly int DetailedDistanceProperty = Shader.PropertyToID("scatter_detailed_distance");
        private static readonly int TileSizeProperty = Shader.PropertyToID("scatter_tile_size");

        public bool Enabled
        {
            get => placeRocks;
            set => placeRocks = value;
        }

        public RockSettings Settings => settings;

        public int Count => _count;

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IRocks>().FromInstance(this);
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (!placeRocks || !settings || !placement || _terrain == null || !_terrain.IsReady || !camera)
            {
                if (!placeRocks)
                {
                    ReleaseAll();
                }
                return;
            }

            Subscribe();
            EnsureRoot();
            if (_layersDirty)
            {
                RebuildLayers();
            }
            if (_layerTable.MaxDensity <= 0)
            {
                return;
            }

            Vector3 cameraTerrain = camera.transform.position - _terrain.Origin;
            UpdateTiles(new Vector2(cameraTerrain.x, cameraTerrain.z));
            if (!_requestInFlight)
            {
                Request(new Vector2(cameraTerrain.x, cameraTerrain.z));
            }
            ApplyResults();
            CoverGround();
        }

        // The ground the spawned rocks cover, for the stones and grass placed later this frame
        private void CoverGround()
        {
            if (_cover == null)
            {
                return;
            }
            if (_coverDirty)
            {
                _coverDirty = false;
                _coverData.Clear();
                foreach (Tile tile in _tiles.Values)
                {
                    foreach (Rock rock in tile.Rocks)
                    {
                        if (rock.Cover.z > 0)
                        {
                            _coverData.Add(rock.Cover);
                        }
                    }
                }
                ScatterUtility.EnsureBuffer(ref _coverBuffer, Mathf.Max(1, _coverData.Count), sizeof(float) * 4);
                if (_coverData.Count > 0)
                {
                    _coverBuffer.SetData(_coverData);
                }
            }
            if (_coverData.Count == 0)
            {
                return;
            }

            ComputeShader shader = placement;
            if (_coverKernel < 0)
            {
                _coverKernel = shader.FindKernel("CoverRocks");
            }
            _cover.Bind(shader, _coverKernel, ScatterCoverUse.WriteLarge);
            shader.SetBuffer(_coverKernel, CoverProperty, _coverBuffer);
            shader.SetInt(CoverCountProperty, _coverData.Count);
            shader.SetVector(OriginProperty, _terrain.Origin);
            // One group per rock; a dispatch dimension takes 65535 groups at most
            shader.Dispatch(_coverKernel, Mathf.Min(_coverData.Count, 65535), 1, 1);
        }

        private void EnsureRoot()
        {
            if (!_root)
            {
                _root = new GameObject("Rocks").transform;
            }
            // Rocks are kept in terrain space
            Vector3 origin = _terrain.Origin;
            if (_root.position != origin)
            {
                _root.position = origin;
            }
        }

        // Tiles within range exist, tiles past the keep range lose their rocks
        private void UpdateTiles(Vector2 camera)
        {
            float size = settings.TileSize;
            float range = settings.Range;
            float keep = range * settings.KeepRange;

            _released.Clear();
            foreach (Tile tile in _tiles.Values)
            {
                if (TileDistance(tile.Coord, camera, size) > keep)
                {
                    _released.Add(tile.Coord);
                }
            }
            foreach (Vector2Int coord in _released)
            {
                Tile tile = _tiles[coord];
                ReleaseRocks(tile.Rocks);
                tile.Rocks.Clear();
                _tiles.Remove(coord);
            }

            Vector2Int min = Vector2Int.FloorToInt((camera - Vector2.one * range) / size);
            Vector2Int max = Vector2Int.FloorToInt((camera + Vector2.one * range) / size);
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    var coord = new Vector2Int(x, y);
                    if (!_tiles.ContainsKey(coord) && TileDistance(coord, camera, size) <= range)
                    {
                        _tiles.Add(coord, new Tile { Coord = coord });
                    }
                }
            }
        }

        private static float TileDistance(Vector2Int coord, Vector2 point, float size)
        {
            Vector2 tileMin = (Vector2)coord * size;
            Vector2 nearest = Vector2.Max(tileMin, Vector2.Min(point, tileMin + Vector2.one * size));
            return Vector2.Distance(nearest, point);
        }

        // Places the nearest tiles that need it on the GPU and reads them back
        private void Request(Vector2 camera)
        {
            _candidates.Clear();
            foreach (Tile tile in _tiles.Values)
            {
                if (!tile.Requested && tile.PlacedVersion != _heightsVersion && tile.NotLoadedVersion != _heightsVersion)
                {
                    _candidates.Add(tile);
                }
            }
            if (_candidates.Count == 0)
            {
                return;
            }

            float size = settings.TileSize;
            _candidates.Sort((a, b) => TileDistance(a.Coord, camera, size).CompareTo(TileDistance(b.Coord, camera, size)));
            int tileCount = Mathf.Min(_candidates.Count, settings.TilesPerRequest);
            int maxPerTile = settings.MaxPerTile;

            if (_requestTiles == null || _requestTiles.Length != settings.TilesPerRequest)
            {
                _requestTiles = new Tile[settings.TilesPerRequest];
            }
            var coords = new Vector2Int[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                _requestTiles[i] = _candidates[i];
                _candidates[i].Requested = true;
                coords[i] = _candidates[i].Coord;
            }
            ScatterUtility.EnsureBuffer(ref _tileBuffer, settings.TilesPerRequest, sizeof(int) * 2);
            ScatterUtility.EnsureBuffer(ref _instanceBuffer, settings.TilesPerRequest * maxPerTile, RockInstance.Stride);
            ScatterUtility.EnsureBuffer(ref _countBuffer, settings.TilesPerRequest, sizeof(uint));
            _tileBuffer.SetData(coords);

            ComputeShader shader = placement;
            if (_kernel < 0)
            {
                _kernel = shader.FindKernel("PlaceRocks");
            }
            _terrain.Bind(shader, _kernel);
            _layerTable.Bind(shader, _kernel);
            if (_areas != null)
            {
                _areas.Bind(shader, _kernel);
            }
            else
            {
                ScatterUtility.EnsureBuffer(ref _emptyAreas, 1, ScatterSystem.AreaStride);
                ScatterUtility.EnsureBuffer(ref _emptyPolygons, 1, sizeof(float) * 2);
                ScatterSystem.BindNone(shader, _kernel, _emptyAreas, _emptyPolygons);
            }
            ScatterUtility.EnsureBuffer(ref _emptyCover, 1, sizeof(uint));
            ScatterSystem.BindNoCover(shader, _kernel, _emptyCover);

            // No thinning or fading with distance: every candidate of a tile counts
            shader.SetVector(CameraProperty, Vector3.zero);
            shader.SetFloat(RangeProperty, 1e9f);
            shader.SetFloat(FadeProperty, 1e-4f);
            shader.SetFloat(FullDensityDistanceProperty, 1e9f);
            shader.SetFloat(DetailedDistanceProperty, 0);
            shader.SetFloat(TileSizeProperty, size);
            shader.SetInt(MaxPerTileProperty, maxPerTile);
            shader.SetBuffer(_kernel, LayersProperty, _layers);
            shader.SetBuffer(_kernel, LayerPrefabsProperty, _layerPrefabs);
            shader.SetBuffer(_kernel, PrefabHalfWidthProperty, _prefabHalfWidthBuffer);
            shader.SetBuffer(_kernel, TilesProperty, _tileBuffer);
            shader.SetBuffer(_kernel, InstancesProperty, _instanceBuffer);
            shader.SetBuffer(_kernel, CountsProperty, _countBuffer);
            shader.Dispatch(_kernel, tileCount, 1, 1);

            _requestInFlight = true;
            _requestTileCount = tileCount;
            _requestVersion = _heightsVersion;
            int id = ++_requestId;
            _instancesRead = false;
            _countsRead = false;
            AsyncGPUReadback.Request(_instanceBuffer, tileCount * maxPerTile * RockInstance.Stride, 0, request =>
            {
                if (this && id == _requestId)
                {
                    OnInstancesRead(request);
                }
            });
            AsyncGPUReadback.Request(_countBuffer, tileCount * sizeof(uint), 0, request =>
            {
                if (this && id == _requestId)
                {
                    OnCountsRead(request);
                }
            });
        }

        private void OnInstancesRead(AsyncGPUReadbackRequest request)
        {
            DisposeReadData(ref _readInstances);
            if (!request.hasError)
            {
                _readInstances = new NativeArray<RockInstance>(request.GetData<RockInstance>(), Allocator.Persistent);
            }
            _instancesRead = true;
            FinishRequestWhenRead(request.hasError);
        }

        private void OnCountsRead(AsyncGPUReadbackRequest request)
        {
            DisposeReadData(ref _readCounts);
            if (!request.hasError)
            {
                _readCounts = new NativeArray<uint>(request.GetData<uint>(), Allocator.Persistent);
            }
            _countsRead = true;
            FinishRequestWhenRead(request.hasError);
        }

        private void FinishRequestWhenRead(bool failed)
        {
            if (!_instancesRead || !_countsRead)
            {
                return;
            }
            _requestInFlight = false;

            int maxPerTile = _readInstances.IsCreated && _requestTileCount > 0 ? _readInstances.Length / _requestTileCount : 0;
            for (int t = 0; t < _requestTileCount; t++)
            {
                Tile tile = _requestTiles[t];
                _requestTiles[t] = null;
                tile.Requested = false;
                // Released meanwhile: dropped. Placed for older heights: applied all the same, so rocks show up while
                // chunks keep loading, and the tile is asked again for the current heights
                if (failed || !_readCounts.IsCreated || !_readInstances.IsCreated || !_tiles.TryGetValue(tile.Coord, out Tile live) ||
                    live != tile)
                {
                    continue;
                }

                uint count = _readCounts[t];
                if (count == NotLoaded)
                {
                    tile.NotLoadedVersion = _requestVersion;
                    continue;
                }

                var result = new TileResult { Tile = tile, Version = _requestVersion };
                for (int r = 0; r < count && r < maxPerTile; r++)
                {
                    result.Rocks.Add(_readInstances[t * maxPerTile + r]);
                }
                _results.Enqueue(result);
            }
            DisposeReadData(ref _readInstances);
            DisposeReadData(ref _readCounts);
        }

        private static void DisposeReadData<T>(ref NativeArray<T> data) where T : struct
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }
        }

        // Applies read back tiles, within the spawn budget of the frame (at least one tile)
        private void ApplyResults()
        {
            int budget = settings.SpawnsPerFrame;
            while (_results.Count > 0 && budget > 0)
            {
                TileResult result = _results.Dequeue();
                Tile tile = result.Tile;
                if (!_tiles.TryGetValue(tile.Coord, out Tile live) || live != tile)
                {
                    continue;
                }
                budget -= Apply(tile, result.Rocks);
                tile.PlacedVersion = result.Version;
            }
        }

        // Keeps the rocks of the tile that stay (same id and prefab), moves them if needed, and replaces the others.
        // Returns the rocks spawned or moved
        private int Apply(Tile tile, List<RockInstance> placed)
        {
            _oldRocks.Clear();
            foreach (Rock rock in tile.Rocks)
            {
                _oldRocks[rock.Id] = rock;
            }
            tile.Rocks.Clear();

            int work = 0;
            foreach (RockInstance instance in placed)
            {
                int prefab = (int)instance.Prefab;
                if (prefab < 0 || prefab >= _prefabs.Count || !_prefabs[prefab])
                {
                    continue;
                }

                GameObject target;
                if (_oldRocks.TryGetValue(instance.Id, out Rock old) && old.Prefab == prefab && old.Instance)
                {
                    _oldRocks.Remove(instance.Id);
                    target = old.Instance;
                }
                else
                {
                    target = Spawn(prefab);
                    work++;
                }

                if (SetTransform(target.transform, instance, _prefabs[prefab].transform.localScale))
                {
                    work++;
                }
                tile.Rocks.Add(new Rock
                {
                    Id = instance.Id,
                    Prefab = prefab,
                    Instance = target,
                    Cover = new Vector4(instance.Position.x, instance.Position.z, instance.Cover, 0),
                });
            }

            foreach (Rock rock in _oldRocks.Values)
            {
                Release(rock);
            }
            _oldRocks.Clear();
            _coverDirty = true;
            return work;
        }

        // True when it moved
        private static bool SetTransform(Transform target, RockInstance instance, Vector3 prefabScale)
        {
            var rotation = new Quaternion(instance.Rotation.x, instance.Rotation.y, instance.Rotation.z, instance.Rotation.w);
            Vector3 scale = prefabScale * instance.Scale;
            if ((target.localPosition - instance.Position).sqrMagnitude < PositionTolerance * PositionTolerance &&
                1 - Mathf.Abs(Quaternion.Dot(target.localRotation, rotation)) < RotationTolerance &&
                (target.localScale - scale).sqrMagnitude < PositionTolerance * PositionTolerance)
            {
                return false;
            }
            target.SetLocalPositionAndRotation(instance.Position, rotation);
            target.localScale = scale;
            return true;
        }

        private GameObject Spawn(int prefab)
        {
            _count++;
            if (_pools.TryGetValue(prefab, out Stack<GameObject> pool))
            {
                while (pool.Count > 0)
                {
                    GameObject pooled = pool.Pop();
                    if (pooled)
                    {
                        pooled.SetActive(true);
                        return pooled;
                    }
                }
            }
            return Instantiate(_prefabs[prefab], _root);
        }

        private void Release(Rock rock)
        {
            if (!rock.Instance)
            {
                return;
            }
            _count--;
            rock.Instance.SetActive(false);
            if (!_pools.TryGetValue(rock.Prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _pools.Add(rock.Prefab, pool);
            }
            pool.Push(rock.Instance);
        }

        private void ReleaseRocks(List<Rock> rocks)
        {
            foreach (Rock rock in rocks)
            {
                Release(rock);
            }
            _coverDirty = true;
        }

        private void ReleaseAll()
        {
            foreach (Tile tile in _tiles.Values)
            {
                ReleaseRocks(tile.Rocks);
                tile.Rocks.Clear();
            }
            _tiles.Clear();
            _results.Clear();
            _coverDirty = true;
        }

        // Layers, prefabs and their GPU data. Every rock goes: prefab indices change
        private void RebuildLayers()
        {
            _layersDirty = false;
            ReleaseAll();
            DestroyPools();
            _heightsVersion++;

            _prefabs.Clear();
            _prefabHalfWidths.Clear();
            _layerData.Clear();
            _layerPrefabData.Clear();
            _layerTable.Build(_terrain.Surfaces, settings.Layers, (_, layer) =>
            {
                int prefabStart = _layerPrefabData.Count;
                foreach (GameObject prefab in layer.Prefabs)
                {
                    if (!prefab)
                    {
                        continue;
                    }
                    int index = _prefabs.IndexOf(prefab);
                    if (index < 0)
                    {
                        index = _prefabs.Count;
                        _prefabs.Add(prefab);
                        _prefabHalfWidths.Add(HalfWidth(prefab));
                    }
                    _layerPrefabData.Add((uint)index);
                }

                // Normal y is the cosine of the slope: the steepest slope gives the smallest
                _layerData.Add(new LayerData
                {
                    Scale = layer.Scale,
                    SlopeCos = new Vector2(Mathf.Cos(layer.Slope.y * Mathf.Deg2Rad) - 1e-4f,
                        Mathf.Cos(layer.Slope.x * Mathf.Deg2Rad) + 1e-4f),
                    SlopeAlignment = layer.SlopeAlignment,
                    MaxRotation = layer.MaxRotation * Mathf.Deg2Rad,
                    Sink = layer.Sink,
                    PrefabStart = (uint)prefabStart,
                    PrefabCount = (uint)(_layerPrefabData.Count - prefabStart),
                    Cover = layer.GroundCover,
                });
            }, HasPrefab);

            // At least one element: every buffer a kernel reads must be bound
            if (_layerData.Count == 0)
            {
                _layerData.Add(default);
            }
            if (_layerPrefabData.Count == 0)
            {
                _layerPrefabData.Add(0);
            }
            if (_prefabHalfWidths.Count == 0)
            {
                _prefabHalfWidths.Add(0);
            }
            ScatterUtility.EnsureBuffer(ref _layers, _layerData.Count, LayerData.Stride);
            ScatterUtility.EnsureBuffer(ref _layerPrefabs, _layerPrefabData.Count, sizeof(uint));
            ScatterUtility.EnsureBuffer(ref _prefabHalfWidthBuffer, _prefabHalfWidths.Count, sizeof(float));
            _layers.SetData(_layerData);
            _layerPrefabs.SetData(_layerPrefabData);
            _prefabHalfWidthBuffer.SetData(_prefabHalfWidths);
        }

        // Half the larger of the width and depth of the meshes of a prefab, as its root is spawned at scale 1
        private static float HalfWidth(GameObject prefab)
        {
            Transform root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, root.localScale) * root.worldToLocalMatrix;
            var bounds = new Bounds();
            bool found = false;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh)
                {
                    continue;
                }
                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 toScaledRoot = toRoot * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 point = toScaledRoot.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, sign));
                    if (found)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }
            return found ? 0.5f * Mathf.Max(bounds.size.x, bounds.size.z) : 0.5f;
        }

        private static bool HasPrefab(RockLayer layer)
        {
            foreach (GameObject prefab in layer.Prefabs)
            {
                if (prefab)
                {
                    return true;
                }
            }
            return false;
        }

        private void DestroyPools()
        {
            foreach (Stack<GameObject> pool in _pools.Values)
            {
                foreach (GameObject pooled in pool)
                {
                    ScatterUtility.DestroyObject(pooled);
                }
            }
            _pools.Clear();
        }

        private void Subscribe()
        {
            if (_subscribedTerrain != _terrain)
            {
                if (_subscribedTerrain != null)
                {
                    _subscribedTerrain.SurfacesChanged -= OnLayersChanged;
                    _subscribedTerrain.HeightsChanged -= OnHeightsChanged;
                }
                _subscribedTerrain = _terrain;
                _subscribedTerrain.SurfacesChanged += OnLayersChanged;
                _subscribedTerrain.HeightsChanged += OnHeightsChanged;
                _layersDirty = true;
            }
            if (_subscribedSettings != settings)
            {
                if (_subscribedSettings)
                {
                    _subscribedSettings.Changed -= OnLayersChanged;
                }
                _subscribedSettings = settings;
                _subscribedSettings.Changed += OnLayersChanged;
                _layersDirty = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedTerrain != null)
            {
                _subscribedTerrain.SurfacesChanged -= OnLayersChanged;
                _subscribedTerrain.HeightsChanged -= OnHeightsChanged;
                _subscribedTerrain = null;
            }
            if (_subscribedSettings)
            {
                _subscribedSettings.Changed -= OnLayersChanged;
                _subscribedSettings = null;
            }
        }

        private void OnLayersChanged() => _layersDirty = true;

        private void OnHeightsChanged() => _heightsVersion++;

        private void OnDestroy()
        {
            Unsubscribe();
            _requestId++;
            DisposeReadData(ref _readInstances);
            DisposeReadData(ref _readCounts);
            _layerTable.Dispose();
            _layers?.Release();
            _layerPrefabs?.Release();
            _prefabHalfWidthBuffer?.Release();
            _coverBuffer?.Release();
            _tileBuffer?.Release();
            _instanceBuffer?.Release();
            _countBuffer?.Release();
            _emptyAreas?.Release();
            _emptyPolygons?.Release();
            _emptyCover?.Release();
            if (_root)
            {
                ScatterUtility.DestroyObject(_root.gameObject);
            }
        }
    }
}
