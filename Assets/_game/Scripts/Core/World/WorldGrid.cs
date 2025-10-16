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

    public class WorldGrid : MonoBehaviour, ILoadAtStart, IInstallerWithContainer
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
        [Inject(Id = "Player")] private TransformTracker _playerTracker;
        private Grid _grid;
        private Dictionary<Vector3Int, LinkedList<IWorldEntity>> _entitiesGrid = new ();
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

        private void RefreshGrid()
        {
            _grid = new Grid(_playerTracker.Position, Settings.occlusionGridCellSize, true);
            _refreshNeighboursRadius = Mathf.RoundToInt(GameData.Data.lodDistances.GetLodDistance(Settings.maxRefreshLod) / Settings.occlusionGridCellSize + 0.5f);
        }
        
        public void AddEntity(IWorldEntity entity)
        {
            var cell = _grid.PositionToCell(entity.Position);

            _entitiesList.Add(entity);
            _coordinatesCache.Add(cell);
            
            AddEntityToCell(cell, entity);
            _lods[entity] = -1;
            SetLodForEntity(entity);
        }

        private void AddEntityToCell(Vector3Int cell, IWorldEntity entity)
        {
            if (!_entitiesGrid.TryGetValue(cell, out var list))
            {
                list = new LinkedList<IWorldEntity>();
                _entitiesGrid[cell] = list;
            }
            list.AddLast(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
            var cell = _grid.PositionToCell(entity.Position);
            _entitiesGrid[cell].Remove(entity);
            _entitiesList.Remove(entity);
            _lods[entity] = -1;
        }

        public int GetLod(IWorldEntity entity)
        {
            return _lods[entity];
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

            _grid.Update(_playerTracker.Position, out _);

            foreach (var entity in EnumerateNeighbours(_playerTracker.Position, _refreshNeighboursRadius))
            {
                SetLodForEntity(entity);
            }
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
                _entitiesGrid[_coordinatesCache[i]].Remove(entity);
                _coordinatesCache[i] = cell;
                AddEntityToCell(cell, entity);
            }
        }

        private void SetLodForEntity(IWorldEntity entity)
        {
            float dSqr = Vector3.SqrMagnitude(entity.Position - _playerTracker.Position);
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
                if (Vector3.SqrMagnitude(worldEntity.Position - _playerTracker.Position) < sqrRadius)
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
                for (cell.y = min.y; cell.y <= max.y; cell.y++)
                {
                    for (cell.z = min.z; cell.z <= max.z; cell.z++)
                    {
                        foreach (var worldEntity in EnumerateCell(cell))
                        {
                            yield return worldEntity;
                        }
                    }
                }   
            }
        }

        public IEnumerable<IWorldEntity> EnumerateCell(Vector3Int cell)
        {
            if (_entitiesGrid.TryGetValue(cell, out var list))
            {
                foreach (var worldEntity in list)
                {
                    yield return worldEntity;
                }
            }
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<WorldGrid>().FromInstance(this);
        }
    }
}