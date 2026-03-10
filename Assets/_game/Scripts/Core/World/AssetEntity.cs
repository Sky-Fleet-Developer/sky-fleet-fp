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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

namespace Core.World
{
    public class AssetEntity : IWorldEntity
    {
        private string _assetId;
        private List<IWorldEntityDisposeListener> _listeners = new();
        private Vector3 _positionCache;
        private Quaternion _rotationCache;
        private Task<GameObject> _loading;
        private AsyncOperationHandle<GameObject> _loadingOp;
        private int _lod;
        private bool _isLodDirty;
        private GameObject _objectInstance;
        public int Id { get; } = IWorldEntity.IdCounter++;

        public Vector3 Position => _positionCache;
        
        public AssetEntity(){}

        public AssetEntity(string assetId, Vector3 position, Quaternion rotation)
        {
            _assetId = assetId;
            _positionCache = position;
            _rotationCache = rotation;
        }

        public AssetEntity(GameObject instance, string assetId)
        {
            _assetId = assetId;
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
                    _loadingOp = Addressables.LoadAssetAsync<GameObject>(_assetId);
                    _loading = _loadingOp.Task;
                    _objectInstance = UnityEngine.Object.Instantiate(await _loading, _positionCache, _rotationCache);
                    _objectInstance.transform.position = _positionCache;
                    _objectInstance.transform.rotation = _rotationCache;
                }
            }
            else
            {
                if (_objectInstance != null)
                {
                    if (_loadingOp.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadingOp.Release();
                    }

                    _loading?.Dispose();
                    _loading = null;
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
            if (_loadingOp.Status == AsyncOperationStatus.Succeeded)
            {
                _loadingOp.Release();
            }
            _loading?.Dispose();
            _loading = null;
        }
        
        public class Serializer : ISerializer<AssetEntity>
        {
            public void Serialize(AssetEntity entity, Stream stream)
            {
                try
                {
                    stream.WriteString(entity._assetId);
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

            public void Populate(Stream stream, ref AssetEntity entity)
            {
                entity._assetId = stream.ReadString();
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