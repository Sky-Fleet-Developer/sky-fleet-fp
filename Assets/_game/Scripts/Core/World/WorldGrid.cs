using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Core.World
{
    [Serializable]
    public class WorldGridData
    {
        public float occlusionGridCellSize;
        public int maxRefreshLod;
        public int refreshPeriod;
    }

    public class WorldGrid : MonoBehaviour, ILoadAtStart, IInstallerWithContainer, IWorldEntityDisposeListener
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
        private List<Vector3Int> _coordinatesCache = new ();
        private int _refreshCounter;
        private int _refreshNeighboursRadius;

        public void SetProfile(WorldGridProfile value)
        {
            profile = value;
            RefreshGrid();
        }
        
        public Task Load()
        {
            RefreshGrid();
            return Task.CompletedTask;
        }
        
        public void AddEntity(IWorldEntity entity)
        {
            var cell = _grid.PositionToCell(entity.Position);

            _entitiesList.Add(entity);
            _coordinatesCache.Add(cell);
            
            _chunksSet.AddEntityToChunk(new Vector2Int(cell.x, cell.z), entity);
            _lods[entity] = -1;
            SetLodForEntity(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
            var cell = _grid.PositionToCell(entity.Position);
            _chunksSet.RemoveEntityFromChunk(new Vector2Int(cell.x, cell.z), entity);
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

            if (_grid.Update(_playerTracker.WorldPosition, out var cell))
            {
                _chunksSet.SetRange(new RectInt(cell.x - _refreshNeighboursRadius, cell.z - _refreshNeighboursRadius,
                    _refreshNeighboursRadius * 2, _refreshNeighboursRadius * 2));
            }

            foreach (var entity in EnumerateNeighbours(_playerTracker.WorldPosition, _refreshNeighboursRadius))
            {
                SetLodForEntity(entity);
            }
        }
        
        private async void RefreshGrid()
        {
            _grid = new Grid(_playerTracker.WorldPosition, Settings.occlusionGridCellSize, true);
            _refreshNeighboursRadius = Mathf.RoundToInt(GameData.Data.lodDistances.GetLodDistance(Settings.maxRefreshLod) / Settings.occlusionGridCellSize + 0.5f);
            var cell = _grid.PositionToCell(_playerTracker.WorldPosition);
            await _chunksSet.SetRange(new RectInt(cell.x - _refreshNeighboursRadius, cell.z - _refreshNeighboursRadius,
                _refreshNeighboursRadius * 2, _refreshNeighboursRadius * 2));
        }
        
        private void UpdateEntity(int i)
        {
            var entity = _entitiesList[i];
            var cell = _grid.PositionToCell(entity.Position);
            if (_coordinatesCache[i] != cell)
            {
                if (_lods[entity] <= Settings.maxRefreshLod) // Sets the lod to the entities which leave the radius of update, which cant be updated in EnumerateNeighbours() enumeration
                {
                    if (_grid.GetDistance(cell) > _refreshNeighboursRadius)
                    {
                        _lods[entity] = Settings.maxRefreshLod + 1;
                        entity.OnLodChanged(Settings.maxRefreshLod + 1);
                    }
                }
                _chunksSet.RemoveEntityFromChunk(new Vector2Int(_coordinatesCache[i].x, _coordinatesCache[i].z), entity);
                _coordinatesCache[i] = cell;
                _chunksSet.AddEntityToChunk(new Vector2Int(cell.x, cell.z), entity);
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
            var range = Vector3Int.one * cellsRadius;
            var min = _grid.PositionToCell(center) - range;
            var max = min + range * 2;
            Vector3Int cell = min;
            for (cell.x = min.x; cell.x <= max.x; cell.x++)
            {
                //for (cell.y = min.y; cell.y <= max.y; cell.y++)
                //{
                    for (cell.z = min.z; cell.z <= max.z; cell.z++)
                    {
                        foreach (var worldEntity in EnumerateCell(cell))
                        {
                            yield return worldEntity;
                        }
                    }
                //}   
            }
        }

        public IEnumerable<IWorldEntity> EnumerateCell(Vector3Int cell)
        {
            var cell2d = new Vector2Int(cell.x, cell.z);
            if (_chunksSet.IsInRange(cell2d))
            {
                foreach (var worldEntity in _chunksSet.GetEntities(cell2d))
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