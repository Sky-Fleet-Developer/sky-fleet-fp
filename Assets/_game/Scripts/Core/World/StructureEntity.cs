using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Data;
using Core.Graph;
using Core.Items;
using Core.Structure;
using Core.Structure.Serialization;
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
            if (lod < GameData.Data.lodDistances.lods.Length && _structure == null)
            {
                ConstructStructure();
            }
            OnLodChangedEvent?.Invoke(this, lod);
        }
        
        public Task GetAnyLoad() => _loading.IsCompleted ? Task.CompletedTask : _loading;

        public Task Serialize(Stream stream)
        {
            Serializer serializer = StructureProvider.GetSerializer();
            var bundle = new StructureBundle(_head, _configs, serializer);
            //TODO
            return Task.CompletedTask;
        }

        public Task Deserialize(Stream stream)
        {
            //TODO
            return Task.CompletedTask;
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
            CycleService.RegisterEntity(this);
            _isConstructInProgress = false;
        }

        private void DestructStructure()
        {
            CycleService.UnregisterEntity(this);
            _destructor.Destruct(_structure);
        }
    }
}