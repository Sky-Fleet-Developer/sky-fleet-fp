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
        private ItemInstance _itemInstance;
        
        public event Action<ItemEntity, int> OnLodChangedEvent;
        public GameObject GameObject => _objectInstance?.transform.gameObject;
        public ItemInstance ItemInstance => _itemInstance;

        public Vector3 Position => _objectInstance == null ? _positionCache : _objectInstance.transform.position + WorldOffset.Offset;
        
        public void Initialize()
        {
            _itemInstance = _itemInstanceFactory.Create(_itemsTable.GetItem(_itemDescription.signId), _itemDescription.amount);
            // TODO: process itemDescription nested items
        }
        
        public void OnLodChanged(int lod)
        {
            _lod = lod;
            if (_isLodDirty) return;
            _isLodDirty = true;
            if (_loading == null)
            {
                ChangeLod();
            }
            else
            {
                _loading.ContinueWith(_ => ChangeLod());
            }
        }
        
        private void ChangeLod()
        {
            if (_lod < GameData.Data.lodDistances.lods.Length)
            {
                if(_objectInstance == null)
                {
                    _loading = _itemObjectFactory.CreateSingle(_itemInstance);
                    _loading.ContinueWith(task => _objectInstance = task.Result);
                }
            }
            else
            {
                if (_objectInstance != null)
                {
                    _itemObjectFactory.Deconstruct(_objectInstance);
                    _objectInstance = null;
                }
            }
            OnLodChangedEvent?.Invoke(this, _lod);
            _isLodDirty = false;
        }

        public Task GetAnyLoad()
        {
            throw new NotImplementedException();
        }
        
        public void RegisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            throw new NotImplementedException();
        }

        public void UnregisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            throw new NotImplementedException();
        }

        
        public class Serializer : ISerializer<ItemEntity>
        {
            private static readonly ISerializer ItemDescriptionSerializer = Serializers.GetSerializer(typeof(ItemDescription));
            
            public static readonly JsonConverter[] Converters = new JsonConverter[]
            {
                new VectorConverter(),
                new QuaternionConverter(),
                new Matrix4x4Converter(),
            };

            public void Serialize(ItemEntity entity, Stream stream)
            {
                try
                {
                    ItemDescriptionSerializer.Serialize(entity._itemDescription, stream);
                    stream.WriteString(JsonConvert.SerializeObject(entity._positionCache, Converters));
                    stream.WriteString(JsonConvert.SerializeObject(entity._rotationCache, Converters));
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
                entity._positionCache = JsonConvert.DeserializeObject<Vector3>(stream.ReadString());
                entity._rotationCache = JsonConvert.DeserializeObject<Quaternion>(stream.ReadString());
            }
        }

        public void Dispose()
        {
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