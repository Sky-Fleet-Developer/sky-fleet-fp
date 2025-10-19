using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure;
using Core.Structure.Serialization;
using Core.World;
using Zenject;

namespace Runtime.Structure
{
    public class StructuresLogisticsInstaller : MonoInstaller
    {
        private StructureFactory _structureFactory = new StructureFactory();
        [Inject]
        private void Inject()
        {
            Container.Inject(_structureFactory);
        }
        
        public override void InstallBindings()
        {
            Container.Bind<IFactory<StructureConfigurationHead, IEnumerable<Configuration<IStructure>>, Task<IStructure>>>().FromInstance(_structureFactory);
            Container.Bind<IStructureDestructor>().FromInstance(_structureFactory);
        }
    }
}