using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Structure;
using Core.Structure.Serialization;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Structure
{
    public class StructuresLogisticsInstaller : MonoBehaviour, IMyInstaller
    {
        private StructureFactory _structureFactory = new StructureFactory();
        private DiContainer _container;

        [Inject]
        private void Inject()
        {
            _container.Inject(_structureFactory);
        }
        
        public void InstallBindings(DiContainer container)
        {
            _container = container;
            _container.Bind<IStructureFactory>().FromInstance(_structureFactory);
        }
    }
}