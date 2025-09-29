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

        public void RegisterStructure(StructureConfiguration blocksConfiguration, GraphConfiguration graphConfiguration)
        {
            StructureEntity entity = new StructureEntity(blocksConfiguration, graphConfiguration);
            _grid.AddEntity(entity);
        }

        public void RegisterStructure(IStructure structure)
        {
            StructureEntity entity = new StructureEntity(structure);
            _grid.AddEntity(entity);
        }
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<WorldSpace>().FromInstance(this);
        }
    }
}