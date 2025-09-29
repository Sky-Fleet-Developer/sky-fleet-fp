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
        private StructureDestructor _structureDestructor = new StructureDestructor();
        [Inject]
        private void Inject()
        {
            Container.Inject(_structureFactory);
            Container.Inject(_structureDestructor);
        }
        
        public override void InstallBindings()
        {
            Container.Bind<IFactory<StructureCreationRuntimeInfo, StructureConfiguration, GraphConfiguration,
                Task<IStructure>>>().FromInstance(_structureFactory);
            Container.Bind<IStructureDestructor>().FromInstance(_structureDestructor);
        }
    }
}