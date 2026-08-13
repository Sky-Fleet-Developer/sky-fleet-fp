using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Core.TerrainGenerator
{
    public class HeightmapData : IDisposable
    {
        private RenderTexture _texture;
        private ComputeBuffer _loadDataBuffer;
        private ComputeBuffer _mapBuffer;
        private int2[] _mapDataContent;
        private TaskCompletionSource<bool> _loadDataTask;
        private bool _mapDirty = true;
        private int _maxChunksSide;
        private int _chunkResolution;

        private Vector2Int _currentMapMin;

        // Пул свободных ячеек (позиций) на самой RenderTexture
        private Queue<int2> _freeTextureSlots;

        // Словарь активных чанков: Мировая координата -> Позиция в текстуре
        private Dictionary<Vector2Int, int2> _activeChunks;

        // Очередь для вытеснения самых старых чанков, если лимит N*N превышен
        private Queue<Vector2Int> _chunkInsertionOrder;
        public RenderTexture Texture => _texture;
        public Vector2Int MapMin => _currentMapMin;

        public HeightmapData(int maxChunksSide, int chunkResolution)
        {
            _maxChunksSide = maxChunksSide;
            _chunkResolution = chunkResolution;

            // Размер текстуры должен быть N*res по обоим осям, чтобы вместить сетку N x N.
            int texSize = maxChunksSide * chunkResolution;
            _texture = new RenderTexture(texSize, texSize, 0, GraphicsFormat.R16_SNorm);
            _texture.enableRandomWrite = true; // Полезно, если планируешь писать в неё через Compute Shader
            _texture.filterMode = FilterMode.Bilinear;
            _texture.Create();

            int maxChunksTotal = maxChunksSide * maxChunksSide;
            _mapDataContent = new int2[maxChunksTotal];

            // Буфер содержит int2 (2 инта = 8 байт)
            _mapBuffer = new ComputeBuffer(maxChunksTotal, sizeof(int) * 2);

            _activeChunks = new Dictionary<Vector2Int, int2>(maxChunksTotal);
            _chunkInsertionOrder = new Queue<Vector2Int>(maxChunksTotal);

            // Инициализируем пул всех доступных позиций на сетке текстуры
            _freeTextureSlots = new Queue<int2>(maxChunksTotal);
            for (int y = 0; y < maxChunksSide; y++)
            {
                for (int x = 0; x < maxChunksSide; x++)
                {
                    _freeTextureSlots.Enqueue(new int2(x, y));
                }
            }

            //int half = chunkResolution * chunkResolution / 2 + (chunkResolution * chunkResolution % 2);
            _loadDataBuffer = new ComputeBuffer(chunkResolution * chunkResolution, 4);
        }

        private Queue<TaskCompletionSource<bool>> _queue = new();

        public async Task<ComputeBuffer> GetLoadDataBuffer()
        {
            while (_queue.Count > 0)
            {
                await _queue.Peek().Task;
            }

            _queue.Enqueue(new TaskCompletionSource<bool>());
            return _loadDataBuffer;
        }

        public void ReleaseLoadDataBuffer()
        {
            _queue.Dequeue().SetResult(true);
        }

        public bool SetChunkToMap(Vector2Int coord, out Vector2Int keyToReleaseAfterUse)
        {
            // Значение по умолчанию, означающее, что ничего вытеснять не пришлось
            keyToReleaseAfterUse = new Vector2Int(int.MinValue, int.MinValue);

            if (_activeChunks.ContainsKey(coord))
                return true; // Чанк уже загружен в память

            // Если свободных слотов нет, вытесняем самый старый чанк (FIFO)
            if (_freeTextureSlots.Count == 0)
            {
                if (_chunkInsertionOrder.Count > 0)
                {
                    keyToReleaseAfterUse = _chunkInsertionOrder.Dequeue();
                    int2 freedSlot = _activeChunks[keyToReleaseAfterUse];

                    _activeChunks.Remove(keyToReleaseAfterUse);
                    _freeTextureSlots.Enqueue(freedSlot);
                }
                else
                {
                    return false; // Непредвиденная ситуация
                }
            }

            // Забираем свободный слот под новый чанк
            int2 slot = _freeTextureSlots.Dequeue();
            Debug.Log($"Bind chunk {coord} to slot {slot}");
            _activeChunks.Add(coord, slot);
            _chunkInsertionOrder.Enqueue(coord);

            _mapDirty = true;
            return true;
        }

        public void ReleaseChunk(Vector2Int xy)
        {
            if (_activeChunks.TryGetValue(xy, out int2 slot))
            {
                _activeChunks.Remove(xy);
                _freeTextureSlots.Enqueue(slot);
                _mapDirty = true;
                Debug.Log($"Release chunk {xy}");
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
                    _mapDataContent[i] = new int2(-1, -1);
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
                        _mapDataContent[index] = kvp.Value;
                    }
                }

                _mapBuffer.SetData(_mapDataContent);
                _mapDirty = false;
            }

            mapMin = _currentMapMin;
            return _mapBuffer;
        }

        public void Dispose()
        {
            _texture.Release();
            UnityEngine.Object.Destroy(_texture);
            _loadDataBuffer?.Dispose();
            _mapBuffer?.Dispose();
        }
    }
}