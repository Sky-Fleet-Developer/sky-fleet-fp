using System;
using Core.Structure;
using Core.Structure.Serialization;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class WorldSpace : MonoBehaviour, IInstallerWithContainer
    {
        [Inject] private WorldGrid _grid;
        private DiContainer _diContainer;

        public void RegisterStructure(StructureConfigurationHead head, Configuration<IStructure>[] configs)
        {
            StructureEntity entity = new StructureEntity(head, configs);
            _diContainer.Inject(entity);
            _grid.AddEntity(entity);
        }

        public void RegisterStructure(IStructure structure)
        {
            StructureEntity entity = new StructureEntity(structure, _diContainer);
            _diContainer.Inject(entity);
            _grid.AddEntity(entity);
        }
        
        public void InstallBindings(DiContainer container)
        {
            _diContainer = container;
            _diContainer.Bind<WorldSpace>().FromInstance(this);
        }
    }
}