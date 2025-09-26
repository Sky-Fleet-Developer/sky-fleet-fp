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
        private struct WorldGridEntity
        {
            public Vector3 Position;
            public Vector3Int Cell;
            public bool IsDisposed;

            public WorldGridEntity(Vector3 position, Vector3Int cell)
            {
                Position = position;
                this.Cell = cell;
                IsDisposed = false;
            }
            
            public void SetPosition(Vector3 position)
            {
                Position = position;
            }
            public void Dispose()
            {
                IsDisposed = true;
            }

            public void SetCell(Vector3Int cell)
            {
                Cell = cell;
            }
        }
        
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
        private TrackerGrid _playerTrackerGrid;
        private Dictionary<Vector3Int, LinkedList<int>> _entitiesGrid;
        private List<WorldGridEntity> _entitiesList = new ();
        private int _entitiesPointer;
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
            _playerTrackerGrid = new TrackerGrid(_playerTracker.Position, Settings.occlusionGridCellSize, true);
        }

        public void Update()
        {
            if (_refreshCounter++ >= Settings.refreshPeriod)
            {
                _refreshCounter = 0;
                for (int i = 0; i < _entitiesList.Count; i++)
                {
                    var entity = _entitiesList[i];
                    var cell = _playerTrackerGrid.PositionToCell(entity.Position);
                    if (entity.Cell != cell)
                    {
                        _entitiesGrid[entity.Cell].Remove(i);
                        entity.Cell = cell;
                        _entitiesList[i] = entity;
                        _entitiesGrid[entity.Cell].AddLast(i);
                    }
                }
            }
        }

        public int AddEntity(Vector3 position)
        {
            int entityIndex = AddEntityPrivate(position);
            var cell = _entitiesList[entityIndex].Cell;
            if (!_entitiesGrid.TryGetValue(cell, out var list))
            {
                list = new LinkedList<int>();
                _entitiesGrid[cell] = list;
            }
            list.AddLast(entityIndex);
            return entityIndex;
        }
        
        private int AddEntityPrivate(Vector3 position)
        {
            var entity = new WorldGridEntity(position, _playerTrackerGrid.PositionToCell(position));
            if (_entitiesList.Count == _entitiesPointer)
            {
                _entitiesList.Add(entity);
                return _entitiesPointer++;
            }
            _entitiesList[_entitiesPointer] = entity;
            for (; _entitiesPointer < _entitiesList.Count; _entitiesPointer++)
            {
                if (_entitiesList[_entitiesPointer].IsDisposed)
                {
                    break;
                }
            }

            return _entitiesPointer;
        }
        
        public void SetPosition(Vector3 worldPosition, int id) => _entitiesList[id].SetPosition(worldPosition);

        public void DisposeEntity(int id)
        {
            _entitiesPointer = id;
            _entitiesGrid[_entitiesList[id].Cell].Remove(id);
            _entitiesList[id].Dispose();
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<WorldGrid>().FromInstance(this);
        }
    }
}