using System.Collections.Generic;
using System.Threading.Tasks;
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
        [Inject] private IFactory<StructureConfigurationHead, IEnumerable<Configuration>, Task<IStructure>> _constructor;
        private IStructure _structure;
        private bool _isConstructInProgress;
        private Configuration[] _configs;
        private StructureConfigurationHead _head;
        public Vector3 Position => _head.position;
        public IStructure Structure => _structure;

        public StructureEntity(StructureConfigurationHead head, Configuration[] configs)
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
                ConstructStructure();
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
            _structure = await _constructor.Create(_head, _configs);
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