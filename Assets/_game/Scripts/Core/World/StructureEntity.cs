using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.Data;
using Core.Structure;
using Core.Structure.Serialization;
using Core.Utilities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class StructureEntity : IWorldEntity
    {
        [Inject] private IStructureDestructor _destructor;
        [Inject] private IFactory<StructureConfigurationHead, IEnumerable<Configuration<IStructure>>, Task<IStructure>> _constructor;
        private IStructure _structure;
        private bool _isConstructInProgress;
        private Configuration<IStructure>[] _configs;
        private StructureConfigurationHead _head;
        private Task<IStructure> _loading;
        private List<IWorldEntityDisposeListener> _disposeListeners = new (2);
        public event Action<StructureEntity, int> OnLodChangedEvent;
        public Vector3 Position => _head.position;
        public IStructure Structure => _structure;

        public StructureEntity() {}
        public StructureEntity(StructureConfigurationHead head, Configuration<IStructure>[] configs)
        {
            _head = head;
            _configs = configs;
        }
        public StructureEntity(IStructure structure, DiContainer diContainer)
        {
            _destructor = diContainer.Resolve<IStructureDestructor>();
            _configs = _destructor.GetDefaultConfigurations(structure, out _head);
        }
        
        public void OnLodChanged(int lod)
        {
            if (lod < GameData.Data.lodDistances.lods.Length)
            {
                if(_structure == null)
                {
                    ConstructStructure();
                }
            }
            else
            {
                if (_structure != null)
                {
                    DestructStructure();
                }
            }
            OnLodChangedEvent?.Invoke(this, lod);
        }
        
        public Task GetAnyLoad() => _loading is { IsCompleted: true } ? Task.CompletedTask : _loading;

        public static readonly JsonConverter[] Converters = new JsonConverter[]
        {
            new VectorConverter(),
            new VectorConverter(),
            new QuaternionConverter(),
            new Matrix4x4Converter(),
        };
        public Task Serialize(Stream stream)
        {
            try
            {
                string headString = JsonConvert.SerializeObject(_head, Converters);
                stream.WriteString(headString);
                stream.WriteInt(_configs.Length);
                foreach (Configuration<IStructure> configuration in _configs)
                {
                    stream.WriteString(configuration.GetType().FullName);
                    string configString = JsonConvert.SerializeObject(configuration, Converters);
                    stream.WriteString(configString);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return Task.CompletedTask;
        }

        public Task Deserialize(Stream stream)
        {
            try
            {
                string headString = stream.ReadString();
                _head = JsonConvert.DeserializeObject<StructureConfigurationHead>(headString);
                
                int configsCount = stream.ReadInt();
                _configs = new Configuration<IStructure>[configsCount];
                for (int i = 0; i < configsCount; i++)
                {
                    string typeName = stream.ReadString();
                    var type = TypeExtensions.GetTypeByName(typeName);
                    if (type == null)
                    {
                        continue;
                    }

                    var config = stream.ReadString();
                    _configs[i] = (Configuration<IStructure>)JsonConvert.DeserializeObject(config, type);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return Task.CompletedTask;
        }

        public void RegisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            _disposeListeners.Add(listener);   
        }

        public void UnregisterDisposeListener(IWorldEntityDisposeListener listener)
        {
            _disposeListeners.Remove(listener);   
        }

        public void Update()
        {
            if (_structure != null)
            {
                _head.position = _structure.transform.position - WorldOffset.Offset;
            }
        }
        /*public void OnDistanceToPlayerChanged(int cellsDistance, float realDistanceSqr)
        {
            if (cellsDistance > GameData.Data.worldEntitiesLoadCellDistance)
            {
                if (_structure != null)
                {
                    _position = _structure.transform.position - WorldOffset.Offset;
                    _rotation = _structure.transform.rotation;
                }
            }
            else
            {
                if (_structure == null && !_isConstructInProgress)
                {
                    ConstructStructure();
                }
            }
        }*/


        private async void ConstructStructure()
        {
            _isConstructInProgress = true;
            _loading = _constructor.Create(_head, _configs);
            _structure = await _loading;
            if (Application.isPlaying)
            {
                CycleService.RegisterEntity(this);
            }
            _isConstructInProgress = false;
        }

        private void DestructStructure()
        {
            if (Application.isPlaying)
            {
                CycleService.UnregisterEntity(this);
            }
            _destructor.Destruct(_structure);
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

            if (_structure != null)
            {
                DestructStructure();
            }
        }
    }
}