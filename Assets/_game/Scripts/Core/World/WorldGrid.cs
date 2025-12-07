using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Data;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

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

    public class WorldGrid : MonoBehaviour, ILoadAtStart, IMyInstaller, IWorldEntityDisposeListener
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
        private Grid _grid;
        private Dictionary<IWorldEntity, int> _lods = new ();
        private List<IWorldEntity> _entitiesList = new ();
        private List<VectorInt> _coordinatesCache = new ();
        private int _refreshCounter;
        private int _refreshNeighboursRadius;
        private bool _isActive = true;
        bool ILoadAtStart.enabled => _isActive;

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
            await RefreshGrid();
        }
        
        public void AddEntity(IWorldEntity entity)
        {
            #if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
            #else
            VectorInt cell = _grid.PositionToCell(entity.Position);
            #endif

            _entitiesList.Add(entity);
            _coordinatesCache.Add(cell);
            
            _chunksSet.AddEntityToChunk(cell, entity);
            _lods[entity] = -1;
            SetLodForEntity(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
#if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
#else
            VectorInt cell = _grid.PositionToCell(entity.Position);
#endif
            _chunksSet.RemoveEntityFromChunk(cell, entity);
            _entitiesList.Remove(entity);
            _lods[entity] = -1;
        }

        public int GetLod(IWorldEntity entity)
        {
            return _lods[entity];
        }

        public float GetCellSize()
        {
            return _grid.Size;
        }

        public void Update()
        {
            if (_refreshCounter++ >= Settings.refreshPeriod)
            {
                _refreshCounter = 0;
                Parallel.For(0, _entitiesList.Count, UpdateEntity);
                /*for (int i = 0; i < _entitiesList.Count; i++)
                {
                    UpdateEntity(i);
                }*/
            }

            if (_grid.Update(_playerTracker.WorldPosition, out Vector3Int cell3d))
            {
#if FLAT_SPACE
                VectorInt cell = new VectorInt(cell3d.x, cell3d.z);
#else
                VectorInt cell = cell3d;
#endif
                _chunksSet.SetRange(new VolumeInt(cell - VectorInt.one * _refreshNeighboursRadius, VectorInt.one * (_refreshNeighboursRadius * 2))).Forget();
            }

            foreach (var entity in EnumerateNeighbours(_playerTracker.WorldPosition, _refreshNeighboursRadius))
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
            await _chunksSet.SetRange(new VolumeInt(cell - VectorInt.one * _refreshNeighboursRadius, VectorInt.one * (_refreshNeighboursRadius * 2)));
#endif
        }
        
        private void UpdateEntity(int i)
        {
            var entity = _entitiesList[i];
#if FLAT_SPACE
            VectorInt cell = new VectorInt(_grid.PositionToCell(entity.Position.x), _grid.PositionToCell(entity.Position.z));
#else
            VectorInt cell = _grid.PositionToCell(entity.Position);
#endif
            if (_coordinatesCache[i] != cell)
            {
                if (_lods[entity] <= Settings.maxRefreshLod) // Sets the lod to the entities which leave the radius of update, which cant be updated in EnumerateNeighbours() enumeration
                {
#if FLAT_SPACE
                    float distance = _grid.GetDistance(new Vector3Int(cell.x, 0, cell.y));
#else
                    float distance = _grid.GetDistance(cell);
#endif
                    if (distance > _refreshNeighboursRadius)
                    {
                        _lods[entity] = Settings.maxRefreshLod + 1;
                        entity.OnLodChanged(Settings.maxRefreshLod + 1);
                    }
                }
                _chunksSet.RemoveEntityFromChunk(_coordinatesCache[i], entity);
                _coordinatesCache[i] = cell;
                _chunksSet.AddEntityToChunk(cell, entity);
            }
        }

        private void SetLodForEntity(IWorldEntity entity)
        {
            float dSqr = Vector3.SqrMagnitude(entity.Position - _playerTracker.WorldPosition);
            var lod = GameData.Data.lodDistances.GetLodSqr(dSqr);
            if (_lods[entity] != lod)
            {
                _lods[entity] = lod;
                entity.OnLodChanged(lod);
            }
        }

        public IEnumerable<IWorldEntity> EnumerateRadius(Vector3 center, float radius)
        {
            var range = Mathf.RoundToInt(radius / _grid.Size + 0.5f);
            float sqrRadius = radius * radius;
            foreach (var worldEntity in EnumerateNeighbours(center, range))
            {
                if (Vector3.SqrMagnitude(worldEntity.Position - _playerTracker.WorldPosition) < sqrRadius)
                {
                    yield return worldEntity;
                }
            }
        }
        
        public IEnumerable<IWorldEntity> EnumerateNeighbours(Vector3 center, int cellsRadius)
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
                            yield return worldEntity;
                        }
#if !FLAT_SPACE
                    }
#endif
                }
            }
        }

        public IEnumerable<IWorldEntity> EnumerateCell(VectorInt cell)
        {
            if (_chunksSet.IsInRange(cell))
            {
                foreach (var worldEntity in _chunksSet.GetEntities(cell))
                {
                    yield return worldEntity;
                }
            }
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<WorldGrid>().FromInstance(this);
        }

        public void OnEntityDisposed(IWorldEntity entity)
        {
            RemoveEntity(entity);
        }
    }
}