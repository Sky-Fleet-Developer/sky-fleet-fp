using System.Collections.Generic;
using Core;
using Core.Scatter;
using Core.TerrainGenerator;
using Core.Trees;
using Runtime.PlacedPrefabs;
using Runtime.Scatter;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Runtime.Trees
{
    /// <summary>
    /// The far ring of trees: every tree up to <see cref="TreeSettings.FarRange"/> is drawn as an impostor quad
    /// (<see cref="TreeImpostor"/> of its prefab) from where the models are culled. The trees of the tiles around the
    /// camera are placed by the same kernel and layers as the near ring (PlacedPrefabPlacement.compute, a
    /// <see cref="PlacedPrefabLayerSet"/> of the same settings), so a tree stands in the same place in both rings, but
    /// they stay on the GPU: tiles live in the slots of a pool and are never read back. Every frame the instances
    /// that are seen are collected per prefab and drawn with one indirect draw each (Visual/Trees/Impostor.hlsl).
    /// </summary>
    public class TreeFarField : MonoBehaviour, IMyInstaller, ITreeFarField
    {
        private class Tile
        {
            public Vector2Int Coord;
            public int Slot;
            /// <summary>Heights version it was placed for; -1 none yet.</summary>
            public int PlacedVersion = -1;
        }

        private class Batch
        {
            public int Prefab;
            public MaterialPropertyBlock Properties;
        }

        // Matches PlacedInstance and FarInstance in PlacedPrefabPlacement.compute
        private const int PlacedInstanceStride = sizeof(float) * 11;
        private const int FarInstanceStride = sizeof(float) * 8;
        private const int IndicesPerQuad = 6;

        [SerializeField] private ComputeShader placement;
        [SerializeField] private Material material;
        [SerializeField] private bool draw = true;

        [Inject(Optional = true)] private ITerrainGpuData _terrain;
        [Inject(Optional = true)] private IScatterGpuAreas _areas;
        [Inject(Optional = true)] private ITrees _trees;

        private readonly Dictionary<Vector2Int, Tile> _tiles = new ();
        private readonly List<Tile> _candidates = new ();
        private readonly List<Vector2Int> _released = new ();
        private readonly Stack<int> _freeSlots = new ();
        private readonly List<Batch> _batches = new ();
        private readonly PlacedPrefabLayerSet _layerSet = new ();
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustum = new Vector4[6];
        private readonly uint[] _zero = new uint[1];
        private Vector4[] _prefabParams = new Vector4[1];
        private Mesh _quads;
        private int _capacity;
        private int _slotCount;
        private int _maxPerTile;
        private int _heightsVersion;
        private bool _layersDirty = true;
        private bool _drawDirty = true;
        private ITerrainGpuData _subscribedTerrain;
        private TreeSettings _subscribedSettings;
        private int _placeKernel = -1;
        private int _resetKernel;
        private int _cullKernel;
        private int _argsKernel;

        private GraphicsBuffer _instances;
        private GraphicsBuffer _counts;
        private GraphicsBuffer _tileBuffer;
        private GraphicsBuffer _slotBuffer;
        private GraphicsBuffer _prefabBuffer;
        private GraphicsBuffer _visible;
        private GraphicsBuffer _visibleCounts;
        private GraphicsBuffer _drawArgs;
        private GraphicsBuffer _emptyAreas;
        private GraphicsBuffer _emptyPolygons;
        private GraphicsBuffer _emptyCover;

        private static readonly int TilesProperty = Shader.PropertyToID("placed_tiles");
        private static readonly int SlotsProperty = Shader.PropertyToID("placed_slots");
        private static readonly int MaxPerTileProperty = Shader.PropertyToID("placed_max_per_tile");
        private static readonly int InstancesProperty = Shader.PropertyToID("placed_instances");
        private static readonly int CountsProperty = Shader.PropertyToID("placed_counts");
        private static readonly int OriginProperty = Shader.PropertyToID("placed_origin");
        private static readonly int CameraProperty = Shader.PropertyToID("scatter_camera");
        private static readonly int RangeProperty = Shader.PropertyToID("scatter_range");
        private static readonly int FadeProperty = Shader.PropertyToID("scatter_fade");
        private static readonly int FullDensityDistanceProperty = Shader.PropertyToID("scatter_full_density_distance");
        private static readonly int DetailedDistanceProperty = Shader.PropertyToID("scatter_detailed_distance");
        private static readonly int TileSizeProperty = Shader.PropertyToID("scatter_tile_size");
        private static readonly int FarPrefabsProperty = Shader.PropertyToID("far_prefabs");
        private static readonly int FarVisibleProperty = Shader.PropertyToID("far_visible");
        private static readonly int FarVisibleCountsProperty = Shader.PropertyToID("far_visible_counts");
        private static readonly int FarDrawArgsProperty = Shader.PropertyToID("far_draw_args");
        private static readonly int FarFrustumProperty = Shader.PropertyToID("far_frustum");
        private static readonly int FarCameraProperty = Shader.PropertyToID("far_camera");
        private static readonly int FarDistancesProperty = Shader.PropertyToID("far_distances");
        private static readonly int FarCapacityProperty = Shader.PropertyToID("far_capacity");
        private static readonly int FarPrefabCountProperty = Shader.PropertyToID("far_prefab_count");
        private static readonly int FarIndicesProperty = Shader.PropertyToID("far_indices_per_instance");
        // Of the impostor shader (Visual/Trees/Impostor.hlsl), per prefab
        private static readonly int InstanceOffsetProperty = Shader.PropertyToID("far_instance_offset");
        private static readonly int VerticesPerInstanceProperty = Shader.PropertyToID("far_vertices_per_instance");
        private static readonly int ImpostorAlbedoProperty = Shader.PropertyToID("impostor_albedo");
        private static readonly int ImpostorNormalProperty = Shader.PropertyToID("impostor_normal");
        private static readonly int ImpostorGridProperty = Shader.PropertyToID("impostor_grid");
        private static readonly int ImpostorCenterProperty = Shader.PropertyToID("impostor_center");
        private static readonly int ImpostorFadeProperty = Shader.PropertyToID("impostor_fade");

        public bool Enabled
        {
            get => draw;
            set => draw = value;
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<ITreeFarField>().FromInstance(this);
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            TreeSettings settings = _trees?.Settings;
            if (!draw || !settings || !placement || !material || _terrain == null || !_terrain.IsReady || !camera)
            {
                return;
            }

            Subscribe(settings);
            if (_layersDirty)
            {
                RebuildLayers(settings);
            }
            if (_layerSet.MaxDensity <= 0)
            {
                return;
            }

            EnsurePool(settings);
            EnsureDraw(settings);
            if (_batches.Count == 0)
            {
                return;
            }

            Vector3 cameraTerrain = camera.transform.position - _terrain.Origin;
            UpdateTiles(settings, new Vector2(cameraTerrain.x, cameraTerrain.z));
            PlaceTiles(settings, new Vector2(cameraTerrain.x, cameraTerrain.z));
            Cull(camera, settings);
            Draw(camera, settings);
        }

        // Slots for every tile that can be within the keep range at once
        private void EnsurePool(TreeSettings settings)
        {
            float keep = settings.FarRange * settings.KeepRange / settings.TileSize + 1.5f;
            int slotCount = Mathf.CeilToInt(Mathf.PI * keep * keep);
            int maxPerTile = settings.MaxPerTile;
            if (_instances != null && slotCount == _slotCount && maxPerTile == _maxPerTile)
            {
                return;
            }

            ReleaseTiles();
            _slotCount = slotCount;
            _maxPerTile = maxPerTile;
            _instances?.Release();
            _counts?.Release();
            _instances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slotCount * maxPerTile, PlacedInstanceStride);
            _counts = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slotCount, sizeof(uint));
            _counts.SetData(new uint[slotCount]);
            _freeSlots.Clear();
            for (int slot = slotCount - 1; slot >= 0; slot--)
            {
                _freeSlots.Push(slot);
            }
            _heightsVersion++;
        }

        // Tiles within the far range have a slot, tiles past the keep range give theirs back
        private void UpdateTiles(TreeSettings settings, Vector2 camera)
        {
            float size = settings.TileSize;
            float range = settings.FarRange;
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
                FreeSlot(_tiles[coord]);
                _tiles.Remove(coord);
            }

            Vector2Int min = Vector2Int.FloorToInt((camera - Vector2.one * range) / size);
            Vector2Int max = Vector2Int.FloorToInt((camera + Vector2.one * range) / size);
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    var coord = new Vector2Int(x, y);
                    if (_freeSlots.Count == 0 || _tiles.ContainsKey(coord) || TileDistance(coord, camera, size) > range)
                    {
                        continue;
                    }
                    _tiles.Add(coord, new Tile { Coord = coord, Slot = _freeSlots.Pop() });
                }
            }
        }

        private void FreeSlot(Tile tile)
        {
            // A slot nobody uses has no instances to draw
            _counts.SetData(_zero, 0, tile.Slot, 1);
            _freeSlots.Push(tile.Slot);
        }

        private void ReleaseTiles()
        {
            _tiles.Clear();
            _freeSlots.Clear();
        }

        private static float TileDistance(Vector2Int coord, Vector2 point, float size)
        {
            Vector2 tileMin = (Vector2)coord * size;
            Vector2 nearest = Vector2.Max(tileMin, Vector2.Min(point, tileMin + Vector2.one * size));
            return Vector2.Distance(nearest, point);
        }

        // Places the nearest tiles that need it into their slots: the same kernel as the near ring, nothing is read back
        private void PlaceTiles(TreeSettings settings, Vector2 camera)
        {
            _candidates.Clear();
            foreach (Tile tile in _tiles.Values)
            {
                if (tile.PlacedVersion != _heightsVersion)
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
            int count = Mathf.Min(_candidates.Count, settings.FarTilesPerFrame);
            var coords = new Vector2Int[count];
            var slots = new uint[count];
            for (int i = 0; i < count; i++)
            {
                coords[i] = _candidates[i].Coord;
                slots[i] = (uint)_candidates[i].Slot;
                _candidates[i].PlacedVersion = _heightsVersion;
            }
            ScatterUtility.EnsureBuffer(ref _tileBuffer, settings.FarTilesPerFrame, sizeof(int) * 2);
            ScatterUtility.EnsureBuffer(ref _slotBuffer, settings.FarTilesPerFrame, sizeof(uint));
            _tileBuffer.SetData(coords, 0, 0, count);
            _slotBuffer.SetData(slots, 0, 0, count);

            ComputeShader shader = placement;
            if (_placeKernel < 0)
            {
                _placeKernel = shader.FindKernel("PlaceInstancesToSlots");
                _resetKernel = shader.FindKernel("FarReset");
                _cullKernel = shader.FindKernel("FarCull");
                _argsKernel = shader.FindKernel("FarArgs");
            }
            _terrain.Bind(shader, _placeKernel);
            _layerSet.Bind(shader, _placeKernel);
            if (_areas != null)
            {
                _areas.Bind(shader, _placeKernel);
            }
            else
            {
                ScatterUtility.EnsureBuffer(ref _emptyAreas, 1, ScatterSystem.AreaStride);
                ScatterUtility.EnsureBuffer(ref _emptyPolygons, 1, sizeof(float) * 2);
                ScatterSystem.BindNone(shader, _placeKernel, _emptyAreas, _emptyPolygons);
            }
            ScatterUtility.EnsureBuffer(ref _emptyCover, 1, sizeof(uint));
            ScatterSystem.BindNoCover(shader, _placeKernel, _emptyCover);

            // No thinning or fading with distance: every candidate of a tile counts, as in the near ring
            shader.SetVector(CameraProperty, Vector3.zero);
            shader.SetFloat(RangeProperty, 1e9f);
            shader.SetFloat(FadeProperty, 1e-4f);
            shader.SetFloat(FullDensityDistanceProperty, 1e9f);
            shader.SetFloat(DetailedDistanceProperty, 0);
            shader.SetFloat(TileSizeProperty, size);
            shader.SetInt(MaxPerTileProperty, _maxPerTile);
            shader.SetBuffer(_placeKernel, TilesProperty, _tileBuffer);
            shader.SetBuffer(_placeKernel, SlotsProperty, _slotBuffer);
            shader.SetBuffer(_placeKernel, InstancesProperty, _instances);
            shader.SetBuffer(_placeKernel, CountsProperty, _counts);
            shader.Dispatch(_placeKernel, count, 1, 1);
        }

        // The instances that are seen, per prefab
        private void Cull(Camera camera, TreeSettings settings)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            for (int i = 0; i < _planes.Length; i++)
            {
                _frustum[i] = new Vector4(_planes[i].normal.x, _planes[i].normal.y, _planes[i].normal.z, _planes[i].distance);
            }

            ComputeShader shader = placement;
            int prefabCount = _layerSet.Prefabs.Count;
            shader.SetVectorArray(FarFrustumProperty, _frustum);
            shader.SetVector(FarCameraProperty, camera.transform.position);
            shader.SetVector(FarDistancesProperty, new Vector2(FadeStart(settings), settings.FarRange));
            shader.SetInt(FarCapacityProperty, _capacity);
            shader.SetInt(FarPrefabCountProperty, prefabCount);
            shader.SetInt(FarIndicesProperty, IndicesPerQuad);
            shader.SetInt(MaxPerTileProperty, _maxPerTile);
            shader.SetVector(OriginProperty, _terrain.Origin);

            shader.SetBuffer(_resetKernel, FarVisibleCountsProperty, _visibleCounts);
            shader.SetBuffer(_cullKernel, FarPrefabsProperty, _prefabBuffer);
            shader.SetBuffer(_cullKernel, FarVisibleProperty, _visible);
            shader.SetBuffer(_cullKernel, FarVisibleCountsProperty, _visibleCounts);
            shader.SetBuffer(_cullKernel, InstancesProperty, _instances);
            shader.SetBuffer(_cullKernel, CountsProperty, _counts);
            shader.SetBuffer(_argsKernel, FarVisibleCountsProperty, _visibleCounts);
            shader.SetBuffer(_argsKernel, FarDrawArgsProperty, _drawArgs);

            int prefabGroups = Mathf.CeilToInt(prefabCount / 64f);
            shader.Dispatch(_resetKernel, prefabGroups, 1, 1);
            shader.Dispatch(_cullKernel, Mathf.Min(_slotCount, 65535), 1, 1);
            shader.Dispatch(_argsKernel, prefabGroups, 1, 1);
        }

        // Impostors fade in over the models in the last meters before they are culled
        private static float FadeStart(TreeSettings settings) => Mathf.Max(0, settings.CullDistance - settings.FarFadeWidth);

        private void Draw(Camera camera, TreeSettings settings)
        {
            var renderParams = new RenderParams(material)
            {
                worldBounds = new Bounds(camera.transform.position, Vector3.one * (settings.FarRange * 2 + 200)),
                camera = camera,
                layer = gameObject.layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                motionVectorMode = MotionVectorGenerationMode.Camera,
            };
            var fade = new Vector4(FadeStart(settings), Mathf.Max(settings.FarFadeWidth, 1e-3f), settings.FarRange, 0);
            foreach (Batch batch in _batches)
            {
                batch.Properties.SetVector(ImpostorFadeProperty, fade);
                renderParams.matProps = batch.Properties;
                Graphics.RenderMeshIndirect(renderParams, _quads, _drawArgs, 1, batch.Prefab);
            }
        }

        // The prefabs have changed: their impostors are looked up again and every tile is placed again
        private void RebuildLayers(TreeSettings settings)
        {
            _layersDirty = false;
            _drawDirty = true;
            ReleaseTiles();
            // Slots are free again; their old instances are stale
            if (_counts != null)
            {
                _counts.SetData(new uint[_counts.count]);
                for (int slot = _counts.count - 1; slot >= 0; slot--)
                {
                    _freeSlots.Push(slot);
                }
            }
            _heightsVersion++;
            _layerSet.Build(_terrain, settings);
        }

        // Buffers and property blocks of the draws: one per prefab that has an impostor
        private void EnsureDraw(TreeSettings settings)
        {
            int prefabCount = _layerSet.Prefabs.Count;
            if (!_drawDirty && _capacity != 0 && _visible != null && _visible.count == prefabCount * _capacity &&
                _capacity == Mathf.Min(settings.FarMaxVisible, ScatterBatchMesh.MaxVertices / 4))
            {
                return;
            }

            _drawDirty = false;
            _batches.Clear();
            if (prefabCount == 0)
            {
                return;
            }

            if (_quads)
            {
                ScatterUtility.DestroyObject(_quads);
            }
            var quad = new Mesh();
            quad.SetVertices(new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) });
            quad.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            quad.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            // Clockwise seen from the camera: the corners are (-1,-1) (1,-1) (1,1) (-1,1) of its right and up
            quad.SetIndices(new[] { 0, 2, 1, 0, 3, 2 }, MeshTopology.Triangles, 0);
            _capacity = ScatterBatchMesh.FitInstances(quad, settings.FarMaxVisible);
            _quads = ScatterBatchMesh.CreateBatch(quad, _capacity, "Tree impostor quads", false);
            ScatterUtility.DestroyObject(quad);

            _prefabParams = new Vector4[prefabCount];
            ScatterUtility.EnsureBuffer(ref _prefabBuffer, prefabCount, sizeof(float) * 4);
            ScatterUtility.EnsureBuffer(ref _visible, prefabCount * _capacity, FarInstanceStride);
            ScatterUtility.EnsureBuffer(ref _visibleCounts, prefabCount, sizeof(uint));
            _drawArgs?.Release();
            _drawArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured, prefabCount,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[prefabCount];
            for (int i = 0; i < prefabCount; i++)
            {
                args[i].instanceCount = 1;
            }
            _drawArgs.SetData(args);

            IReadOnlyList<GameObject> prefabs = _layerSet.Prefabs;
            for (int i = 0; i < prefabCount; i++)
            {
                var reference = prefabs[i].GetComponent<TreeImpostorReference>();
                TreeImpostor impostor = reference ? reference.Impostor : null;
                if (!impostor || !impostor.Albedo || !impostor.NormalDepth)
                {
                    continue;
                }

                // The center and radius are those of the prefab at its own scale
                float scale = prefabs[i].transform.localScale.y;
                Vector3 center = impostor.Center * scale;
                _prefabParams[i] = new Vector4(center.x, center.y, center.z, impostor.Radius * scale);

                var properties = new MaterialPropertyBlock();
                properties.SetBuffer(FarVisibleProperty, _visible);
                properties.SetFloat(InstanceOffsetProperty, i * _capacity);
                // The batch mesh holds a quad (4 vertices) for every instance: the vertex id tells which
                properties.SetFloat(VerticesPerInstanceProperty, 4);
                properties.SetTexture(ImpostorAlbedoProperty, impostor.Albedo);
                properties.SetTexture(ImpostorNormalProperty, impostor.NormalDepth);
                properties.SetVector(ImpostorGridProperty, new Vector4(impostor.FramesPerSide, 1f / impostor.FramesPerSide, 0, 0));
                properties.SetVector(ImpostorCenterProperty, _prefabParams[i]);
                _batches.Add(new Batch { Prefab = i, Properties = properties });
            }
            _prefabBuffer.SetData(_prefabParams);
        }

        private void Subscribe(TreeSettings settings)
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

        private void OnLayersChanged() => _layersDirty = true;

        private void OnHeightsChanged() => _heightsVersion++;

        private void OnDestroy()
        {
            if (_subscribedTerrain != null)
            {
                _subscribedTerrain.SurfacesChanged -= OnLayersChanged;
                _subscribedTerrain.HeightsChanged -= OnHeightsChanged;
            }
            if (_subscribedSettings)
            {
                _subscribedSettings.Changed -= OnLayersChanged;
            }
            _layerSet.Dispose();
            _instances?.Release();
            _counts?.Release();
            _tileBuffer?.Release();
            _slotBuffer?.Release();
            _prefabBuffer?.Release();
            _visible?.Release();
            _visibleCounts?.Release();
            _drawArgs?.Release();
            _emptyAreas?.Release();
            _emptyPolygons?.Release();
            _emptyCover?.Release();
            ScatterUtility.DestroyObject(_quads);
        }
    }
}
