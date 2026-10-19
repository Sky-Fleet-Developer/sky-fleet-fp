using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.PlacedPrefabs;
using Core.Scatter;
using Core.TerrainGenerator;
using Runtime.Scatter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Zenject;

namespace Runtime.PlacedPrefabs
{
    /// <summary>
    /// Prefab instances (rocks, trees...) around the camera. The terrain is split into tiles; a tile coming into range
    /// has its instances placed on the GPU (PlacedPrefabPlacement.compute: the same candidates, layers, density patches
    /// and areas as the scattered details), read back, and spawned as pooled prefab instances. Instances live in terrain
    /// space under a root following ITerrainGpuData.Origin, so they stay put when the world offset moves. When the
    /// heights are baked again (chunks loaded, generation edited) the tiles are placed again and only the instances
    /// that changed are moved or replaced.
    /// A kind derives from it, supplies its settings and root name, and exposes its own abstraction in Core.
    /// </summary>
    public abstract class PlacedPrefabSpawner : MonoBehaviour, IPlacedPrefabs
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData
        {
            public const int Stride = sizeof(float) * 11;

            public Vector3 Position;
            public float Scale;
            public Vector4 Rotation;
            public uint Prefab;
            public uint Id;
            public float Cover;
        }

        private struct Spawned
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
            /// <summary>Heights version its instances were placed for; -1 none yet.</summary>
            public int PlacedVersion = -1;
            /// <summary>Heights version its terrain wasn't loaded at; asked again at the next one.</summary>
            public int NotLoadedVersion = -1;
            public bool Requested;
            public readonly List<Spawned> Items = new ();
        }

        private class TileResult
        {
            public Tile Tile;
            public int Version;
            public readonly List<InstanceData> Instances = new ();
        }

        private const uint NotLoaded = 0xFFFFFFFF;
        // Moves smaller than these are not applied, so placing a tile again doesn't touch instances that stay
        private const float PositionTolerance = 1e-3f;
        private const float RotationTolerance = 1e-4f;

        [SerializeField] private ComputeShader placement;
        [FormerlySerializedAs("placeRocks")]
        [SerializeField] private bool place = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private IScatterGpuAreas _areas;
        [Inject(Optional = true)] private IScatterGpuCover _cover;

        private readonly Dictionary<Vector2Int, Tile> _tiles = new ();
        private readonly List<Tile> _candidates = new ();
        private readonly List<Vector2Int> _released = new ();
        private readonly Queue<TileResult> _results = new ();
        private readonly Dictionary<int, Stack<GameObject>> _pools = new ();
        private readonly List<Vector4> _coverData = new ();
        private readonly Dictionary<uint, Spawned> _oldItems = new ();
        private readonly PlacedPrefabLayerSet _layerSet = new ();
        private Transform _root;
        private int _kernel = -1;
        private int _coverKernel = -1;
        private bool _coverDirty = true;
        private int _heightsVersion;
        private bool _layersDirty = true;
        private ITerrainGpuData _subscribedTerrain;
        private PlacedPrefabSettings _subscribedSettings;
        private int _count;

        // One request at a time, its buffers kept until it is read back
        private Tile[] _requestTiles;
        private int _requestVersion;
        private int _requestId;
        private int _requestTileCount;
        private bool _requestInFlight;
        private NativeArray<InstanceData> _readInstances;
        private NativeArray<uint> _readCounts;
        private bool _instancesRead;
        private bool _countsRead;

        private GraphicsBuffer _coverBuffer;
        private GraphicsBuffer _tileBuffer;
        private GraphicsBuffer _instanceBuffer;
        private GraphicsBuffer _countBuffer;
        private GraphicsBuffer _emptyAreas;
        private GraphicsBuffer _emptyPolygons;
        private GraphicsBuffer _emptyCover;

        private static readonly int TilesProperty = Shader.PropertyToID("placed_tiles");
        private static readonly int MaxPerTileProperty = Shader.PropertyToID("placed_max_per_tile");
        private static readonly int InstancesProperty = Shader.PropertyToID("placed_instances");
        private static readonly int CountsProperty = Shader.PropertyToID("placed_counts");
        private static readonly int CoverProperty = Shader.PropertyToID("placed_cover");
        private static readonly int CoverCountProperty = Shader.PropertyToID("placed_cover_count");
        private static readonly int OriginProperty = Shader.PropertyToID("placed_origin");
        private static readonly int CameraProperty = Shader.PropertyToID("scatter_camera");
        private static readonly int RangeProperty = Shader.PropertyToID("scatter_range");
        private static readonly int FadeProperty = Shader.PropertyToID("scatter_fade");
        private static readonly int FullDensityDistanceProperty = Shader.PropertyToID("scatter_full_density_distance");
        private static readonly int DetailedDistanceProperty = Shader.PropertyToID("scatter_detailed_distance");
        private static readonly int TileSizeProperty = Shader.PropertyToID("scatter_tile_size");

        public bool Enabled
        {
            get => place;
            set => place = value;
        }

        public int Count => _count;

        /// <summary>Settings of the kind: range, tiles and layers.</summary>
        protected abstract PlacedPrefabSettings PlacedSettings { get; }

        /// <summary>Name of the object the instances are spawned under.</summary>
        protected abstract string RootName { get; }

        protected virtual void Update()
        {
            Camera camera = Camera.main;
            if (!place || !PlacedSettings || !placement || _terrain == null || !_terrain.IsReady || !camera)
            {
                if (!place)
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
            if (_layerSet.MaxDensity <= 0)
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

        // The ground the spawned instances cover, for the stones and grass placed later this frame
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
                    foreach (Spawned item in tile.Items)
                    {
                        if (item.Cover.z > 0)
                        {
                            _coverData.Add(item.Cover);
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
                _coverKernel = shader.FindKernel("CoverInstances");
            }
            _cover.Bind(shader, _coverKernel, ScatterCoverUse.WriteLarge);
            shader.SetBuffer(_coverKernel, CoverProperty, _coverBuffer);
            shader.SetInt(CoverCountProperty, _coverData.Count);
            shader.SetVector(OriginProperty, _terrain.Origin);
            // One group per instance; a dispatch dimension takes 65535 groups at most
            shader.Dispatch(_coverKernel, Mathf.Min(_coverData.Count, 65535), 1, 1);
        }

        private void EnsureRoot()
        {
            if (!_root)
            {
                _root = new GameObject(RootName).transform;
            }
            // Instances are kept in terrain space
            Vector3 origin = _terrain.Origin;
            if (_root.position != origin)
            {
                _root.position = origin;
            }
        }

        // Tiles within range exist, tiles past the keep range lose their instances
        private void UpdateTiles(Vector2 camera)
        {
            float size = PlacedSettings.TileSize;
            float range = PlacedSettings.Range;
            float keep = range * PlacedSettings.KeepRange;

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
                ReleaseItems(tile.Items);
                tile.Items.Clear();
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

            PlacedPrefabSettings settings = PlacedSettings;
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
            ScatterUtility.EnsureBuffer(ref _instanceBuffer, settings.TilesPerRequest * maxPerTile, InstanceData.Stride);
            ScatterUtility.EnsureBuffer(ref _countBuffer, settings.TilesPerRequest, sizeof(uint));
            _tileBuffer.SetData(coords);

            ComputeShader shader = placement;
            if (_kernel < 0)
            {
                _kernel = shader.FindKernel("PlaceInstances");
            }
            _terrain.Bind(shader, _kernel);
            _layerSet.Bind(shader, _kernel);
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
            AsyncGPUReadback.Request(_instanceBuffer, tileCount * maxPerTile * InstanceData.Stride, 0, request =>
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
                _readInstances = new NativeArray<InstanceData>(request.GetData<InstanceData>(), Allocator.Persistent);
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
                // Released meanwhile: dropped. Placed for older heights: applied all the same, so instances show up
                // while chunks keep loading, and the tile is asked again for the current heights
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
                    result.Instances.Add(_readInstances[t * maxPerTile + r]);
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
            int budget = PlacedSettings.SpawnsPerFrame;
            while (_results.Count > 0 && budget > 0)
            {
                TileResult result = _results.Dequeue();
                Tile tile = result.Tile;
                if (!_tiles.TryGetValue(tile.Coord, out Tile live) || live != tile)
                {
                    continue;
                }
                budget -= Apply(tile, result.Instances);
                tile.PlacedVersion = result.Version;
            }
        }

        // Keeps the instances of the tile that stay (same id and prefab), moves them if needed, and replaces the others.
        // Returns the instances spawned or moved
        private int Apply(Tile tile, List<InstanceData> placed)
        {
            _oldItems.Clear();
            foreach (Spawned item in tile.Items)
            {
                _oldItems[item.Id] = item;
            }
            tile.Items.Clear();

            int work = 0;
            foreach (InstanceData instance in placed)
            {
                int prefab = (int)instance.Prefab;
                IReadOnlyList<GameObject> prefabs = _layerSet.Prefabs;
                if (prefab < 0 || prefab >= prefabs.Count || !prefabs[prefab])
                {
                    continue;
                }

                GameObject target;
                if (_oldItems.TryGetValue(instance.Id, out Spawned old) && old.Prefab == prefab && old.Instance)
                {
                    _oldItems.Remove(instance.Id);
                    target = old.Instance;
                }
                else
                {
                    target = Spawn(prefab);
                    work++;
                }

                if (SetTransform(target.transform, instance, prefabs[prefab].transform.localScale))
                {
                    work++;
                }
                tile.Items.Add(new Spawned
                {
                    Id = instance.Id,
                    Prefab = prefab,
                    Instance = target,
                    Cover = new Vector4(instance.Position.x, instance.Position.z, instance.Cover, 0),
                });
            }

            foreach (Spawned item in _oldItems.Values)
            {
                Release(item);
            }
            _oldItems.Clear();
            _coverDirty = true;
            return work;
        }

        // True when it moved
        private static bool SetTransform(Transform target, InstanceData instance, Vector3 prefabScale)
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
            GameObject created = Instantiate(_layerSet.Prefabs[prefab], _root);
            OnInstantiated(created);
            return created;
        }

        /// <summary>A new instance was made (not taken from the pool): a kind sets it up here.</summary>
        protected virtual void OnInstantiated(GameObject instance)
        {
        }

        private void Release(Spawned item)
        {
            if (!item.Instance)
            {
                return;
            }
            _count--;
            item.Instance.SetActive(false);
            if (!_pools.TryGetValue(item.Prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _pools.Add(item.Prefab, pool);
            }
            pool.Push(item.Instance);
        }

        private void ReleaseItems(List<Spawned> items)
        {
            foreach (Spawned item in items)
            {
                Release(item);
            }
            _coverDirty = true;
        }

        private void ReleaseAll()
        {
            foreach (Tile tile in _tiles.Values)
            {
                ReleaseItems(tile.Items);
                tile.Items.Clear();
            }
            _tiles.Clear();
            _results.Clear();
            _coverDirty = true;
        }

        // Layers, prefabs and their GPU data. Every instance goes: prefab indices change
        private void RebuildLayers()
        {
            _layersDirty = false;
            ReleaseAll();
            DestroyPools();
            _heightsVersion++;
            _layerSet.Build(_terrain, PlacedSettings);
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
            if (_subscribedSettings != PlacedSettings)
            {
                if (_subscribedSettings)
                {
                    _subscribedSettings.Changed -= OnLayersChanged;
                }
                _subscribedSettings = PlacedSettings;
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

        protected virtual void OnDestroy()
        {
            Unsubscribe();
            _requestId++;
            DisposeReadData(ref _readInstances);
            DisposeReadData(ref _readCounts);
            _layerSet.Dispose();
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
