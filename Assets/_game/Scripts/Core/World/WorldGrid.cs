using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Data;
using Core.Misc;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;
using ITickable = Core.Misc.ITickable;

#if FLAT_SPACE
using VectorInt = UnityEngine.Vector2Int;
using VolumeInt = UnityEngine.RectInt;
#else
using VectorInt = UnityEngine.Vector3Int;
using VolumeInt = UnityEngine.BoundsInt;
#endif

namespace Core.World
{
    [Serializable]
    public class WorldGridData
    {
        public float occlusionGridCellSize;
        public int maxRefreshLod;
        public int refreshPeriod;
    }

    public class WorldGrid : MonoBehaviour, ITickable, ILoadAtStart, IMyInstaller, IWorldEntityDisposeListener
    {
        [SerializeField] private WorldGridProfile profile;
        [ShowInInspector] private WorldGridData Settings
        {
            get => profile?.data;
            set
            {
                if (!profile)
                {
                    return;
                }
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(profile, "ChangeWorldGridProfile");
#endif
                profile.data = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(profile);
#endif
            }
        }
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;
        [Inject] private LocationChunksSet _chunksSet;
        [Inject] private TickService _tickService;
        private Grid _grid;
        private Dictionary<int, int> _lods = new (); // Key is entityId, value is entity lod
        private Dictionary<int, IWorldEntity> _entities = new ();
        private Dictionary<int, VectorInt> _coordinatesCache = new ();
        private int _refreshCounter;
        private int _refreshNeighboursRadius;
        private bool _isActive = true;
        bool ILoadAtStart.enabled => _isActive;
        public Grid Grid => _grid;

        public event Action<IWorldEntity> OnEntityAdded;
        public event Action<IWorldEntity> OnEntityRemoved;
        public IEnumerable<IWorldEntity> Entities => _entities.Values;
        public int TickRate => 1;

        static WorldGrid()
        {
            TickService.SetUpdate(typeof(WorldGrid), false);
        }

        private void Awake()
        {
            if (_isActive) return;
            _isActive = true;
            gameObject.SetActive(false);
        }

        public Task SetProfile(WorldGridProfile value)
        {
            profile = value;
            return RefreshGrid();
        }
        
        public async Task Load()
        {
            _isActive = true;
            gameObject.SetActive(true);
            _tickService.Add(this);
            await RefreshGrid();
        }

        private void OnDestroy()
        {
            _tickService.Remove(this);
        }

        public void AddEntity(IWorldEntity entity)
        {
            entity.Initialize();
            #if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
            #else
            VectorInt cell = _grid.PositionToCell(entity.Position);
            #endif
            var id = entity.Id;
            _entities.Add(id, entity);
            _coordinatesCache.Add(id, cell);
            
            _chunksSet.AddEntityToChunk(cell, entity);

            _lods[id] = -1;
            SetLodForEntity(entity);
            OnEntityAdded?.Invoke(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
#if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
#else
            VectorInt cell = _grid.PositionToCell(entity.Position);
#endif
            var id = entity.Id;
            _chunksSet.RemoveEntityFromChunk(cell, entity);
            _entities.Remove(id);
            _lods[id] = -1;
            OnEntityRemoved?.Invoke(entity);
        }
        
        public IWorldEntity GetEntity(int id) => _entities[id];

        public int GetLod(IWorldEntity entity)
        {
            return _lods[entity.Id];
        }

        public float GetCellSize()
        {
            return _grid.Size;
        }

        public void Tick()
        {
            if (_grid.Update(_playerTracker.WorldPosition, out Vector3Int cell3d))
            {
#if FLAT_SPACE
                VectorInt cell = new VectorInt(cell3d.x, cell3d.z);
#else
                VectorInt cell = cell3d;
#endif
                _chunksSet.SetRange(new VolumeInt(cell - VectorInt.one * _refreshNeighboursRadius, VectorInt.one * (_refreshNeighboursRadius * 2)));
            }
            
            if (_refreshCounter++ >= Settings.refreshPeriod)
            {
                _refreshCounter = 0;
                Parallel.ForEach(_entities, UpdateEntity);
                /*for (int i = 0; i < _entitiesList.Count; i++)
                {
                    UpdateEntity(i);
                }*/
            }

            foreach ((IWorldEntity entity, VectorInt cell) in EnumerateNeighbours(_playerTracker.WorldPosition, _refreshNeighboursRadius))
            {
                SetLodForEntity(entity);
            }
        }
        
        private async Task RefreshGrid()
        {
            _grid = new Grid(_playerTracker.WorldPosition, Settings.occlusionGridCellSize, true);
            _refreshNeighboursRadius = GameData.Data != null ? Mathf.RoundToInt(GameData.Data.lodDistances.GetLodDistance(Settings.maxRefreshLod) / Settings.occlusionGridCellSize + 0.5f) : (int)(_chunksSet.GetRange().size.magnitude * 0.5f);
            Vector3Int cell = _grid.PositionToCell(_playerTracker.WorldPosition);
#if FLAT_SPACE
            await _chunksSet.SetRange(new VolumeInt(cell.x - _refreshNeighboursRadius, cell.z - _refreshNeighboursRadius,
                _refreshNeighboursRadius * 2, _refreshNeighboursRadius * 2));
#else
            await _chunksSet.SetRangeAsync(new VolumeInt(cell - VectorInt.one * _refreshNeighboursRadius, VectorInt.one * (_refreshNeighboursRadius * 2)));
#endif
        }
        
        private void UpdateEntity(KeyValuePair<int, IWorldEntity> keyValuePair)
        {
#if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
#else
            VectorInt cell = _grid.PositionToCell(keyValuePair.Value.Position);
#endif
            if (_coordinatesCache[keyValuePair.Key] != cell)
            {
                if (_lods[keyValuePair.Key] <= Settings.maxRefreshLod) // Sets the lod to the entities which leave the radius of update, which cant be updated in EnumerateNeighbours() enumeration
                {
#if FLAT_SPACE
                    float distance = _grid.GetDistance(new Vector3Int(cell.x, 0, cell.y));
#else
                    float distance = _grid.GetDistance(cell);
#endif
                    if (distance > _refreshNeighboursRadius)
                    {
                        _lods[keyValuePair.Key] = Settings.maxRefreshLod + 1;
                        keyValuePair.Value.OnLodChanged(Settings.maxRefreshLod + 1);
                    }
                }
                _chunksSet.RemoveEntityFromChunk(_coordinatesCache[keyValuePair.Key], keyValuePair.Value);
                _coordinatesCache[keyValuePair.Key] = cell;
                _chunksSet.AddEntityToChunk(cell, keyValuePair.Value);
            }
        }

        private void SetLodForEntity(IWorldEntity entity)
        {
            float dSqr = Vector3.SqrMagnitude(entity.Position - _playerTracker.WorldPosition);
            var lod = GameData.Data.lodDistances.GetLodSqr(dSqr);
            if (_lods[entity.Id] != lod)
            {
                _lods[entity.Id] = lod;
                entity.OnLodChanged(lod);
            }
        }

        public IEnumerable<(IWorldEntity entity, VectorInt cell)> EnumerateRadius(Vector3 center, float radius)
        {
            var range = Mathf.RoundToInt(radius / _grid.Size + 0.5f);
            float sqrRadius = radius * radius;
            foreach ((IWorldEntity entity, VectorInt cell) in EnumerateNeighbours(center, range))
            {
                if (Vector3.SqrMagnitude(entity.Position - _playerTracker.WorldPosition) < sqrRadius)
                {
                    yield return (entity, cell);
                }
            }
        }
        
        public IEnumerable<(IWorldEntity entity, VectorInt cell)> EnumerateNeighbours(Vector3 center, int cellsRadius)
        {
            var range = VectorInt.one * cellsRadius;
#if FLAT_SPACE
            VectorInt centerCell = new VectorInt(_grid.PositionToCell(center.x), _grid.PositionToCell(center.z));
#else
            VectorInt centerCell = _grid.PositionToCell(center);
#endif
            
            var min = centerCell - range;
            var max = min + range * 2;
            VectorInt cell = min;
            for (cell.x = min.x; cell.x <= max.x; cell.x++)
            {
                for (cell.y = min.y; cell.y <= max.y; cell.y++)
                {
#if !FLAT_SPACE
                    for (cell.z = min.z; cell.z <= max.z; cell.z++)
                    {
#endif
                        foreach (var worldEntity in EnumerateCell(cell))
                        {
                            yield return (worldEntity, cell);
                        }
#if !FLAT_SPACE
                    }
#endif
                }
            }
        }

        public IEnumerable<IWorldEntity> EnumerateCell(VectorInt cell)
        {
            foreach (var worldEntity in _chunksSet.GetEntities(cell))
            {
                yield return worldEntity;
            }
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<WorldGrid>().FromInstance(this);
        }

        void IWorldEntityDisposeListener.OnEntityDisposed(IWorldEntity entity)
        {
            RemoveEntity(entity);
        }
    }
}