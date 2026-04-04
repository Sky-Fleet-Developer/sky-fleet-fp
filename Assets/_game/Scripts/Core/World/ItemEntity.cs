using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Configurations;
using Core.ContentSerializer;
using Core.Data;
using Core.Items;
using Core.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Zenject;
using UniTaskVoid = Cysharp.Threading.Tasks.UniTaskVoid;

namespace Core.World
{
    public class ItemEntity : IObjectEntity
    {
        [Inject] private ItemsTable _itemsTable;
        [Inject] private IItemObjectFactory _itemObjectFactory;
        [Inject] private IItemInstanceFactory _itemInstanceFactory;
        private Vector3 _positionCache;
        private Quaternion _rotationCache;
        private ItemDescription _itemDescription;
        private Task<IItemObject> _loading;
        private List<IWorldEntityDisposeListener> _disposeListeners = new (2);
        private int _lod;
        private bool _isLodDirty;
        private IItemObject _objectInstance;
        private Rigidbody _rigidbody;
        private ItemInstance _itemInstance;
        protected DiContainer OverrideContainer { get; set; } = null;
        public int Id { get; } = IWorldEntity.IdCounter++;
        public event Action<ItemEntity, int> OnLodChangedEvent;
        public GameObject GameObject => _objectInstance?.transform.gameObject;
        public Rigidbody Rigidbody => _rigidbody;

        public ItemInstance ItemInstance => _itemInstance;

        public Vector3 Position => _positionCache;

        public ItemEntity()
        {
        }

        public ItemEntity(ItemDescription itemDescription, Vector3 position, Quaternion rotation) : this()
        {
            _itemDescription = itemDescription;
            _positionCache = position;
            _rotationCache = rotation;
        }
        
        public ItemEntity(IItemObject objectInstance, ItemDescription itemDescription) : this()
        {
            _itemDescription = itemDescription;
            _objectInstance = objectInstance;
            _rigidbody = _objectInstance.transform.GetComponent<Rigidbody>();
            _positionCache = objectInstance.transform.position;
            _rotationCache = objectInstance.transform.rotation;
        }
        
        public virtual void Initialize()
        {
            _itemInstance = _itemInstanceFactory.CreateByDescription(_itemDescription);
            if (_objectInstance is IItemObjectHandle itemObjectHandle)
            {
                _itemObjectFactory.SetupInstance(itemObjectHandle, _itemInstance, OverrideContainer);
            }
        }
        
        public void UpdateTransforms()
        {
            if (_objectInstance != null)
            {
                _positionCache = _objectInstance.transform.position;
                _rotationCache = _objectInstance.transform.rotation;
            }
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
                    _loading = _itemObjectFactory.CreateSingle(_itemInstance, OverrideContainer);
                    OnSpawn(await _loading);
                }
            }
            else
            {
                if (_objectInstance != null)
                {
                    OnDespawn();
                    _itemObjectFactory.Deconstruct(_objectInstance);
                    _objectInstance = null;
                }
            }
            OnLodChangedEvent?.Invoke(this, _lod);
            _isLodDirty = false;
        }
        

        protected virtual void OnSpawn(IItemObject instance)
        {
            _objectInstance = instance;
            _rigidbody = _objectInstance.transform.GetComponent<Rigidbody>();
            _objectInstance.transform.position = _positionCache;
            _objectInstance.transform.rotation = _rotationCache;
        }

        protected virtual void OnDespawn()
        {
        }

        public Task GetAnyLoad()
        {
            return _loading ?? Task.CompletedTask;
        }
        
        public void RegisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            _disposeListeners.Add(listener);
        }

        public void UnregisterDisposeListener(IWorldEntityDisposeListener listener)
        {
             _disposeListeners.Remove(listener);
        }

        public override string ToString()
        {
            return $"ItemEntity {_itemDescription.signId}";
        }

        public class Serializer : ISerializer<ItemEntity>
        {
            private static readonly ISerializer ItemDescriptionSerializer = Serializers.GetSerializer(typeof(ItemDescription));
            
            public void Serialize(ItemEntity entity, Stream stream)
            {
                try
                {
                    ItemDescriptionSerializer.Serialize(entity._itemDescription, stream);
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

            public ItemEntity Deserialize(Stream stream)
            {
                var entity = new ItemEntity();
                Populate(stream, ref entity);
                return entity;
            }

            public void Populate(Stream stream, ref ItemEntity entity)
            {
                entity._itemDescription = (ItemDescription)ItemDescriptionSerializer.Deserialize(stream);
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

        public void Dispose()
        {
            if (_objectInstance != null)
            {
                _itemObjectFactory.Deconstruct(_objectInstance);
            }

            foreach (var listener in _disposeListeners)
            {
                listener.OnEntityDisposed(this);
            }
            if (_loading != null)
            {
                _loading.Dispose();
                _loading = null;
            }
        }
    }
}