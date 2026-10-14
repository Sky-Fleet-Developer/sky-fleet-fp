using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator
{
    public class TerrainData : IDisposable
    {
        private RenderTexture _heightmapTex;
        private RenderTexture _rawHeightmapTex;
        private RenderTexture _scratchHeightmapTex;
        private RenderTexture _alphamapTex;
        private RenderTexture _rawAlphamapTex;
        private RenderTexture _scratchAlphamapTex;
        private ComputeBuffer _loadHeightDataBuffer;
        private ComputeBuffer _loadAlphaDataBuffer;
        private ComputeBuffer _mapBuffer;
        private int[] _mapDataContent;
        private ComputeBuffer _slotCoordsBuffer;
        private int3[] _slotCoordsContent;
        private TaskCompletionSource<bool> _loadDataTask;
        private bool _mapDirty = true;
        private int _maxChunksSide;
        private int _chunkResolution;

        private Vector2Int _currentMapMin;

        // Пул свободных ячеек (позиций) на самой RenderTexture
        private Queue<int2> _freeTextureSlots;

        // Словарь активных чанков: Мировая координата -> Позиция в текстуре
        public Dictionary<Vector2Int, int2> _activeChunks;
        public Dictionary<Vector2Int, int> _versions;

        // Очередь для вытеснения самых старых чанков, если лимит N*N превышен
        private Queue<Vector2Int> _chunkInsertionOrder;
        /// <summary>Baked heights (raw data + heightmap filters). Used by rendering and collision.</summary>
        public RenderTexture HeightmapTex => _heightmapTex;
        /// <summary>Heights exactly as loaded from the chunk files. Input of the heightmap bake.</summary>
        public RenderTexture RawHeightmapTex => _rawHeightmapTex;
        /// <summary>Per texture layer: int3(chunk.x, chunk.y, isUsed). Updated together with the map buffer.</summary>
        public ComputeBuffer SlotCoordsBuffer => _slotCoordsBuffer;
        /// <summary>CPU copy of <see cref="SlotCoordsBuffer"/> for one layer.</summary>
        public int3 GetSlotCoords(int layer) => _slotCoordsContent[layer];
        public int LayerCount => _maxChunksSide * _maxChunksSide;
        public int Resolution => _chunkResolution;
        /// <summary>Raised when new chunk data was uploaded to the raw heightmap and has to be baked.</summary>
        public event Action RawDataChanged;
        /// <summary>Baked alphamap (raw data + filters that write the alphamap). Used by rendering.</summary>
        public RenderTexture AlphamapTex => _alphamapTex;
        /// <summary>Alphamap as blended from the biomes. Input of the bake.</summary>
        public RenderTexture RawAlphamapTex => _rawAlphamapTex;
        public Vector2Int MapMin => _currentMapMin;

        public TerrainData(int maxChunksSide, int chunkResolution)
        {
            _maxChunksSide = maxChunksSide;
            _chunkResolution = chunkResolution;

            _heightmapTex = CreateHeightmapArray();
            _rawHeightmapTex = CreateHeightmapArray();
            _alphamapTex = CreateAlphamapArray();
            _rawAlphamapTex = CreateAlphamapArray();

            int maxChunksTotal = maxChunksSide * maxChunksSide;
            _mapDataContent = new int[maxChunksTotal];

            _mapBuffer = new ComputeBuffer(maxChunksTotal, sizeof(int));
            _slotCoordsContent = new int3[maxChunksTotal];
            _slotCoordsBuffer = new ComputeBuffer(maxChunksTotal, sizeof(int) * 3);

            _activeChunks = new Dictionary<Vector2Int, int2>(maxChunksTotal);
            _chunkInsertionOrder = new Queue<Vector2Int>(maxChunksTotal);
            _versions = new Dictionary<Vector2Int, int>(maxChunksTotal);

            _freeTextureSlots = new Queue<int2>(maxChunksTotal);
            for (int y = 0; y < maxChunksSide; y++)
            {
                for (int x = 0; x < maxChunksSide; x++)
                {
                    _freeTextureSlots.Enqueue(new int2(x, y));
                }
            }

            _loadHeightDataBuffer = new ComputeBuffer(chunkResolution * chunkResolution, 4);
            _loadAlphaDataBuffer = new ComputeBuffer(chunkResolution * chunkResolution, 4);
        }

        private RenderTexture CreateHeightmapArray()
        {
            var texture = new RenderTexture(_chunkResolution, _chunkResolution, -1, GraphicsFormat.R16_SNorm);
            texture.dimension = TextureDimension.Tex2DArray;
            texture.volumeDepth = _maxChunksSide * _maxChunksSide;
            texture.enableRandomWrite = true;
            texture.filterMode = FilterMode.Bilinear;
            texture.Create();
            return texture;
        }

        private RenderTexture CreateAlphamapArray()
        {
            var texture = new RenderTexture(_chunkResolution, _chunkResolution, -1, GraphicsFormat.R8G8B8A8_UNorm);
            texture.volumeDepth = _maxChunksSide * _maxChunksSide;
            texture.dimension = TextureDimension.Tex2DArray;
            texture.enableRandomWrite = true;
            texture.filterMode = FilterMode.Bilinear;
            texture.Create();
            return texture;
        }

        /// <summary>Intermediate target for chaining several filters that write the alphamap. Created on first use.</summary>
        public RenderTexture GetScratchAlphamapTex()
        {
            if (!_scratchAlphamapTex)
            {
                _scratchAlphamapTex = CreateAlphamapArray();
            }
            return _scratchAlphamapTex;
        }

        /// <summary>Intermediate target for chaining several heightmap filters. Created on first use.</summary>
        public RenderTexture GetScratchHeightmapTex()
        {
            if (!_scratchHeightmapTex)
            {
                _scratchHeightmapTex = CreateHeightmapArray();
            }
            return _scratchHeightmapTex;
        }

        public void NotifyRawDataChanged()
        {
            RawDataChanged?.Invoke();
        }

        private Queue<TaskCompletionSource<ComputeBuffer>> _queue = new();
        private TaskCompletionSource<ComputeBuffer> _last;

        public Task<ComputeBuffer> GetLoadDataBuffer()
        {
            var tcs = new TaskCompletionSource<ComputeBuffer>();
            _queue.Enqueue(tcs);
            //Debug.Log($"Enqueue load data => {_queue.Count}");
            var toReturn = _last;
            _last = tcs;
            if (toReturn != null)
            {
                return toReturn.Task;
            }
            else
            {
                return Task.FromResult(_loadHeightDataBuffer);
            }
        }

        public void ReleaseLoadDataBuffer()
        {
            //Debug.Log($"Dequeue load data => {_queue.Count - 1}");
            _queue.Dequeue().SetResult(_loadHeightDataBuffer);
            if (_queue.Count == 0)
            {
                _last = null;
            }
        }

        /// <param name="textureSlot">texture array layer assigned to the chunk</param>
        public bool SetChunkToMap(Vector2Int coord, out Vector3Int keyToReleaseAfterUse, out int textureSlot)
        {
            // Значение по умолчанию, означающее, что ничего вытеснять не пришлось
            keyToReleaseAfterUse = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            textureSlot = -1;

            if (_activeChunks.TryGetValue(coord, out int2 existingSlot))
            {
                keyToReleaseAfterUse = new Vector3Int(coord.x, coord.y, _versions[coord]);
                textureSlot = SlotToLayer(existingSlot);
                return true; // Чанк уже загружен в память
            }

            // Если свободных слотов нет, вытесняем самый старый чанк (FIFO)
            if (_freeTextureSlots.Count == 0)
            {
                if (_chunkInsertionOrder.Count > 0)
                {
                    var releaseKey = _chunkInsertionOrder.Dequeue();
                    int2 freedSlot = _activeChunks[releaseKey];
                    Debug.LogWarning($"Terrain texture slots are full: chunk {releaseKey} evicted for {coord}. Increase TerrainGenerationSettings.maxLoadedChunksByOneSide.");

                    _activeChunks.Remove(releaseKey);
                    _freeTextureSlots.Enqueue(freedSlot);
                }
                else
                {
                    return false; // Непредвиденная ситуация
                }
            }

            // Забираем свободный слот под новый чанк
            int2 slot = _freeTextureSlots.Dequeue();
            //Debug.Log($"Bind chunk {coord} to slot {slot}");
            _activeChunks.Add(coord, slot);
            var version = _versions.GetValueOrDefault(coord, -1) + 1;
            _versions[coord] = version;
            _chunkInsertionOrder.Enqueue(coord);
            keyToReleaseAfterUse = new Vector3Int(coord.x, coord.y, version);
            textureSlot = SlotToLayer(slot);
            _mapDirty = true;
            return true;
        }

        // Must match the layer index written to the map buffer in GetMapBuffer
        private int SlotToLayer(int2 slot) => slot.x * _maxChunksSide + slot.y;

        public void ReleaseChunk(Vector3Int key)
        {
            var xy = new Vector2Int(key.x, key.y);
            if (_versions.TryGetValue(xy, out int version) && version != key.z)
            {
                return;
            }
            if (_activeChunks.Remove(xy, out int2 slot))
            {
                _freeTextureSlots.Enqueue(slot);
                _mapDirty = true;
                //Debug.Log($"Release chunk {xy}");
                // Пересобираем очередь (не самое дешевое действие, но при N <= 32 работает мгновенно)
                RebuildInsertionOrder();
            }
        }

        private void RebuildInsertionOrder()
        {
            var newQueue = new Queue<Vector2Int>(_activeChunks.Count);
            foreach (var coord in _chunkInsertionOrder)
            {
                if (_activeChunks.ContainsKey(coord))
                    newQueue.Enqueue(coord);
            }

            _chunkInsertionOrder = newQueue;
        }

        /// <summary>
        /// Raised right after the map buffer is rebuilt. Anything that caches mapMin
        /// (e.g. material matrices) must refresh it here, or it will address wrong slots.
        /// </summary>
        public event Action MapChanged;

        /// <summary>Rebuilds the map buffer now if chunks were added or released since the last rebuild.</summary>
        public void FlushMap()
        {
            GetMapBuffer(out _, out _);
        }

        public ComputeBuffer GetMapBuffer(out Vector2Int mapMin, out int mapSize)
        {
            mapSize = _maxChunksSide;

            if (_mapDirty)
            {
                // 1. Вычисляем mapMin (левый нижний угол активной области)
                if (_activeChunks.Count > 0)
                {
                    int minX = int.MaxValue;
                    int minY = int.MaxValue;

                    foreach (var key in _activeChunks.Keys)
                    {
                        if (key.x < minX) minX = key.x;
                        if (key.y < minY) minY = key.y;
                    }

                    _currentMapMin = new Vector2Int(minX, minY);
                }
                else
                {
                    _currentMapMin = Vector2Int.zero;
                }

                // 2. Очищаем старые данные (заполняем -1, чтобы в шейдере отловить пустоты)
                for (int i = 0; i < _mapDataContent.Length; i++)
                {
                    _mapDataContent[i] = -1;
                }

                // 3. Заполняем плоский массив карты
                foreach (var kvp in _activeChunks)
                {
                    // Переводим глобальную координату чанка в локальную для карты
                    int localX = kvp.Key.x - _currentMapMin.x;
                    int localY = kvp.Key.y - _currentMapMin.y;

                    // Защита от выхода за пределы скользящего окна N*N
                    if (localX >= 0 && localX < mapSize && localY >= 0 && localY < mapSize)
                    {
                        int index = localX * mapSize + localY;
                        _mapDataContent[index] = SlotToLayer(kvp.Value);
                    }
                }

                _mapBuffer.SetData(_mapDataContent);

                Array.Clear(_slotCoordsContent, 0, _slotCoordsContent.Length);
                foreach (var kvp in _activeChunks)
                {
                    _slotCoordsContent[SlotToLayer(kvp.Value)] = new int3(kvp.Key.x, kvp.Key.y, 1);
                }
                _slotCoordsBuffer.SetData(_slotCoordsContent);
                _mapDirty = false;
                MapChanged?.Invoke();
            }

            mapMin = _currentMapMin;
            return _mapBuffer;
        }
        
        ~TerrainData() => Dispose();

        private static void ReleaseTexture(RenderTexture texture)
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
        }

        public void Dispose()
        {
            ReleaseTexture(_heightmapTex);
            ReleaseTexture(_rawHeightmapTex);
            ReleaseTexture(_scratchHeightmapTex);
            ReleaseTexture(_alphamapTex);
            ReleaseTexture(_rawAlphamapTex);
            ReleaseTexture(_scratchAlphamapTex);
            _slotCoordsBuffer?.Dispose();
            _loadHeightDataBuffer?.Dispose();
            _loadAlphaDataBuffer?.Dispose();
            _mapBuffer?.Dispose();
        }
    }
    
}