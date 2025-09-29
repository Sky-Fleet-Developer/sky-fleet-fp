using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
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
        public float refreshEntitiesDistance;
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
        private List<IWorldEntity> _entitiesList = new ();
        private List<Vector3Int> _coordinatesCache = new ();
        private int _refreshCounter;

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
        }
        
        public void AddEntity(IWorldEntity entity)
        {
            var cell = _grid.PositionToCell(entity.Position);

            _entitiesList.Add(entity);
            _coordinatesCache.Add(cell);
            
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
        }

        private void UpdateEntity(int i)
        {
            var entity = _entitiesList[i];
            var cell = _grid.PositionToCell(entity.Position);
            if (_coordinatesCache[i] != cell)
            {
                _entitiesGrid[_coordinatesCache[i]].Remove(entity);
                _coordinatesCache[i] = cell;
                _entitiesList[i] = entity;
                _entitiesGrid[cell].AddLast(entity);
            }
        }

        public IEnumerable<IWorldEntity> EnumerateRadius(IWorldEntity entity, float radius)
        {
            var range = _grid.PositionToCell(Vector3.one * radius);
            var cell = _grid.PositionToCell(entity.Position) - range;
            var max = cell + range;
            for (; cell.x <= max.x; cell.x++)
            {
                for (; cell.y <= max.y; cell.y++)
                {
                    for (; cell.z <= max.z; cell.z++)
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