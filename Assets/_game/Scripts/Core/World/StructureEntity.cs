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
        public StructureConfiguration BlocksConfiguration;
        public GraphConfiguration GraphConfiguration;
        private Vector3 _position;
        private Quaternion _rotation;
        private IStructure _structure;
        [Inject] private IStructureDestructor _destructor;
        [Inject] private IFactory<StructureCreationRuntimeInfo, StructureConfiguration, GraphConfiguration, Task<IStructure>> _constructor;
        public Vector3 Position => _position;
        private bool _isConstructInProgress;

        public StructureEntity(StructureConfiguration blocksConfiguration, GraphConfiguration graphConfiguration)
        {
            BlocksConfiguration = blocksConfiguration;
            GraphConfiguration = graphConfiguration;
        }
        public StructureEntity(IStructure structure)
        {
            BlocksConfiguration = new StructureConfiguration(structure);
            if (structure is IGraph graph)
            {
                GraphConfiguration = new GraphConfiguration(graph);
            }
            _position = structure.transform.position - WorldOffset.Offset;
        }
        
        public void OnDistanceToPlayerChanged(int cellsDistance, float realDistanceSqr)
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
        }

        private async void ConstructStructure()
        {
            _isConstructInProgress = true;
            var info = new StructureCreationRuntimeInfo { LocalRotation = _rotation, LocalPosition = _position };
            await _constructor.Create(info, BlocksConfiguration, GraphConfiguration);
            _isConstructInProgress = false;
        }
    }
}