using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Configurations;
using Core.ContentSerializer;
using Core.Data;
using Core.Items;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class PrefabEntity : IWorldEntity
    {
        private string _prefabId;
        private List<IWorldEntityDisposeListener> _listeners = new();
        private Vector3 _positionCache;
        private Quaternion _rotationCache;
        private Task<GameObject> _loading;
        private int _lod;
        private bool _isLodDirty;
        private ITablePrefab _objectInstance;

        [Inject] private TablePrefabs _prefabs;
        public Vector3 Position => _positionCache;
        
        public PrefabEntity(){}

        public PrefabEntity(string prefabId, Vector3 position, Quaternion rotation)
        {
            _prefabId = prefabId;
            _positionCache = position;
            _rotationCache = rotation;
        }

        public PrefabEntity(ITablePrefab instance)
        {
            _prefabId = instance.Guid;
            _objectInstance = instance;
            _positionCache = instance.transform.position;
            _rotationCache = instance.transform.rotation;
        }
        
        public void OnLodChanged(int lod)
        {
            _lod = lod;
            if (_isLodDirty) return;
            _isLodDirty = true;
            if (_loading == null)
            {
                ChangeLod().Forget();
            }
            else
            {
                _loading.ContinueWith(_ => ChangeLod().Forget());
            }
        }

        private async UniTaskVoid ChangeLod()
        {
            if (_lod < GameData.Data.lodDistances.lods.Length)
            {
                if(_objectInstance == null)
                {
                    _loading = _prefabs.GetItem(_prefabId).LoadPrefab();
                    _objectInstance = UnityEngine.Object.Instantiate(await _loading, _positionCache, _rotationCache).GetComponent<ITablePrefab>();
                    _objectInstance.transform.position = _positionCache;
                    _objectInstance.transform.rotation = _rotationCache;
                }
            }
            else
            {
                if (_objectInstance != null)
                {
                    UnityEngine.Object.Destroy(_objectInstance.transform.gameObject);
                    _objectInstance = null;
                }
            }
            //OnLodChangedEvent?.Invoke(this, _lod);
            _isLodDirty = false;
        }
        
        public Task GetAnyLoad()
        {
            return Task.CompletedTask;
        }

        public void Initialize()
        {
            
        }

        public void RegisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            _listeners.Add(listener);
        }

        public void UnregisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            _listeners.Remove(listener);
        }
        public void Dispose()
        {
            
        }
        
        public class Serializer : ISerializer<PrefabEntity>
        {
            
            public static readonly JsonConverter[] Converters = new JsonConverter[]
            {
                new VectorConverter(),
                new QuaternionConverter(),
                new Matrix4x4Converter(),
            };

            public void Serialize(PrefabEntity entity, Stream stream)
            {
                try
                {
                    stream.WriteString(entity._prefabId);
                    for (int i = 0; i < 3; i++)
                    {
                        stream.WriteFloat(entity._positionCache[i]);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        stream.WriteFloat(entity._rotationCache[i]);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            public PrefabEntity Deserialize(Stream stream)
            {
                var entity = new PrefabEntity();
                Populate(stream, ref entity);
                return entity;
            }

            public void Populate(Stream stream, ref PrefabEntity entity)
            {
                entity._prefabId = stream.ReadString();
                for (int i = 0; i < 3; i++)
                {
                    entity._positionCache[i] = stream.ReadFloat();
                }
                for (int i = 0; i < 4; i++)
                {
                    entity._rotationCache[i] = stream.ReadFloat();
                }
            }
        }
    }
}