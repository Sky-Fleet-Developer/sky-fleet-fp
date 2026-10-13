using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Source tiles of all biomes in one pair of texture arrays, so a single kernel can sample any biome.
    /// Tiles are identified by <see cref="IBiomeSource.ContentKey"/>, so biomes reading the same data share the tiles.
    /// Tiles are requested every time the requirements change; tiles that are no longer required stay cached
    /// until their layer is needed for another tile.
    /// </summary>
    public class BiomeTilePool : IDisposable
    {
        private enum TileState
        {
            Loading,
            Ready,
            Missing,
        }

        private class Tile
        {
            public Vector2Int Coord;
            public TileState State;
            public int Layer = -1;
            public BiomeTileTarget Target;
            public bool Required;
            public int LastUsedFrame;
        }

        private readonly MapGpuWorker _gpuWorker;
        private readonly int _resolution;
        private readonly int _capacity;
        private readonly Dictionary<string, Dictionary<Vector2Int, Tile>> _tiles = new ();
        private readonly Stack<int> _freeLayers = new ();
        private readonly List<Vector2Int> _removeCache = new ();
        private readonly List<string> _keysCache = new ();
        private RenderTexture _heightTex;
        private RenderTexture _alphaTex;
        private ComputeBuffer _uploadBuffer;
        private bool _capacityWarned;

        /// <summary>Raised on the main thread when a requested tile finished loading.</summary>
        public event Action TileLoaded;

        public RenderTexture HeightTex => _heightTex;
        public RenderTexture AlphaTex => _alphaTex;
        public int Resolution => _resolution;

        public BiomeTilePool(MapGpuWorker gpuWorker, int resolution, int capacity)
        {
            _gpuWorker = gpuWorker;
            _resolution = resolution;
            _capacity = Mathf.Max(1, capacity);
            for (int i = _capacity - 1; i >= 0; i--)
            {
                _freeLayers.Push(i);
            }

            _heightTex = CreateArray(GraphicsFormat.R16_UNorm, "Biome tiles: heights");
            _alphaTex = CreateArray(GraphicsFormat.R8G8B8A8_UNorm, "Biome tiles: alphamap");
        }

        private RenderTexture CreateArray(GraphicsFormat format, string name)
        {
            var texture = new RenderTexture(_resolution, _resolution, 0, format)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = _capacity,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name,
            };
            texture.Create();
            return texture;
        }

        /// <summary>Starts a new set of requirements: every tile becomes not required until requested again.</summary>
        public void BeginRequirements()
        {
            foreach (var tiles in _tiles.Values)
            {
                foreach (Tile tile in tiles.Values)
                {
                    tile.Required = false;
                }
            }
        }

        /// <summary>
        /// Marks a tile as required and starts loading it when it is not in the pool yet.
        /// A tile already loaded or loading for another source with the same content key is reused.
        /// </summary>
        public void Require(IBiomeSource source, Vector2Int coord)
        {
            string key = source.ContentKey;
            if (key == null)
            {
                return;
            }

            if (!_tiles.TryGetValue(key, out var tiles))
            {
                tiles = new Dictionary<Vector2Int, Tile>();
                _tiles.Add(key, tiles);
            }

            if (!tiles.TryGetValue(coord, out Tile tile))
            {
                tile = new Tile { Coord = coord };
                tiles.Add(coord, tile);
                tile.Required = true;
                StartLoading(source, tile);
            }

            tile.Required = true;
            tile.LastUsedFrame = Time.frameCount;
        }

        /// <summary>Forgets missing tiles that are no longer required, and content keys without tiles.</summary>
        public void EndRequirements()
        {
            _keysCache.Clear();
            foreach (var pair in _tiles)
            {
                _removeCache.Clear();
                foreach (Tile tile in pair.Value.Values)
                {
                    if (!tile.Required && tile.State == TileState.Missing)
                    {
                        _removeCache.Add(tile.Coord);
                    }
                }

                foreach (Vector2Int coord in _removeCache)
                {
                    pair.Value.Remove(coord);
                }

                if (pair.Value.Count == 0)
                {
                    _keysCache.Add(pair.Key);
                }
            }

            foreach (string key in _keysCache)
            {
                _tiles.Remove(key);
            }
        }

        /// <summary>False while the tile is still loading. A source without data is always settled.</summary>
        public bool IsSettled(IBiomeSource source, Vector2Int coord)
        {
            string key = source.ContentKey;
            return key == null || _tiles.TryGetValue(key, out var tiles) && tiles.TryGetValue(coord, out Tile tile) && tile.State != TileState.Loading;
        }

        /// <summary>Adds the loaded required tiles of a source: coordinate and texture layer.</summary>
        public void GetReadyTiles(IBiomeSource source, List<(Vector2Int coord, int layer)> result)
        {
            string key = source.ContentKey;
            if (key == null || !_tiles.TryGetValue(key, out var tiles))
            {
                return;
            }

            foreach (Tile tile in tiles.Values)
            {
                if (tile.Required && tile.State == TileState.Ready)
                {
                    result.Add((tile.Coord, tile.Layer));
                }
            }
        }

        private void StartLoading(IBiomeSource source, Tile tile)
        {
            int layer = AllocateLayer();
            if (layer < 0)
            {
                tile.State = TileState.Missing;
                if (!_capacityWarned)
                {
                    _capacityWarned = true;
                    Debug.LogError($"Biome tile pool is full ({_capacity} tiles). Increase TerrainGenerationSettings.biomeTilePoolSize.");
                }
                return;
            }

            tile.Layer = layer;
            tile.State = TileState.Loading;
            tile.Target = new BiomeTileTarget(_heightTex, _alphaTex, layer, _resolution, _gpuWorker, GetUploadBuffer);
            Load(source, tile, tile.Target).Forget();
        }

        private async UniTaskVoid Load(IBiomeSource source, Tile tile, BiomeTileTarget target)
        {
            bool loaded;
            try
            {
                loaded = await source.LoadTile(tile.Coord, target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                loaded = false;
            }

            // Released while loading: the layer may already belong to another tile
            if (!target.IsValid)
            {
                return;
            }

            tile.State = loaded ? TileState.Ready : TileState.Missing;
            if (!loaded)
            {
                FreeLayer(tile);
            }
            TileLoaded?.Invoke();
        }

        private int AllocateLayer()
        {
            if (_freeLayers.Count > 0)
            {
                return _freeLayers.Pop();
            }

            // Reuse the layer of the least recently required cached tile
            Tile oldest = null;
            Dictionary<Vector2Int, Tile> oldestOwner = null;
            foreach (var tiles in _tiles.Values)
            {
                foreach (Tile tile in tiles.Values)
                {
                    if (!tile.Required && tile.Layer >= 0 && (oldest == null || tile.LastUsedFrame < oldest.LastUsedFrame))
                    {
                        oldest = tile;
                        oldestOwner = tiles;
                    }
                }
            }

            if (oldest == null)
            {
                return -1;
            }

            Release(oldest);
            oldestOwner.Remove(oldest.Coord);
            return _freeLayers.Pop();
        }

        private void Release(Tile tile)
        {
            tile.Target?.Invalidate();
            FreeLayer(tile);
        }

        private void FreeLayer(Tile tile)
        {
            if (tile.Layer >= 0)
            {
                _freeLayers.Push(tile.Layer);
                tile.Layer = -1;
            }
        }

        private ComputeBuffer GetUploadBuffer(int count)
        {
            if (_uploadBuffer == null || _uploadBuffer.count < count)
            {
                _uploadBuffer?.Dispose();
                _uploadBuffer = new ComputeBuffer(count, sizeof(float));
            }
            return _uploadBuffer;
        }

        public void Dispose()
        {
            foreach (var tiles in _tiles.Values)
            {
                foreach (Tile tile in tiles.Values)
                {
                    tile.Target?.Invalidate();
                }
            }
            _tiles.Clear();
            ReleaseTexture(ref _heightTex);
            ReleaseTexture(ref _alphaTex);
            _uploadBuffer?.Dispose();
            _uploadBuffer = null;
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (!texture)
            {
                return;
            }

            texture.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            texture = null;
        }
    }
}
