using System;
using Core.ContentSerializer;
using Core.Structure;
using Core.Structure.Serialization;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class WorldSpace : MonoBehaviour, IMyInstaller
    {
        [Inject] private WorldGrid _grid;
        private DiContainer _diContainer;

        public void AddEntity(IWorldEntity entity)
        {
            _diContainer.Inject(entity);
            _grid.AddEntity(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
            _grid.RemoveEntity(entity);
        }

        public void RegisterStructure(StructureConfigurationHead head, params Configuration<IStructure>[] configs)
        {
            AddEntity(new StructureEntity(head, configs));
        }

        public void RegisterStructure(IStructure structure)
        {
            AddEntity(new StructureEntity(structure, _diContainer));
        }
        
        public void InstallBindings(DiContainer container)
        {
            _diContainer = container;
            _diContainer.Bind<WorldSpace>().FromInstance(this);
        }
    }
}