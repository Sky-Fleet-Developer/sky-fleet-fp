using Core.Structure.Serialization;

namespace Core.Structure
{
    public interface IStructureDestructor
    {
        void Destruct(IStructure structure);
        Configuration<IStructure>[] GetDefaultConfigurations(IStructure structure, out StructureConfigurationHead head);
    }
    
}