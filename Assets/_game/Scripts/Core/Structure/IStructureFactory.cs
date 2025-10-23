using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure.Serialization;

namespace Core.Structure
{
    public interface IStructureFactory
    {
        Task<IStructure> Create(StructureConfigurationHead head, IEnumerable<Configuration<IStructure>> configurations);
        void Destruct(IStructure structure);
        Configuration<IStructure>[] GetDefaultConfigurations(IStructure structure, out StructureConfigurationHead head);
    }
    
}