using Core.Structure;
using Core.Utilities;

namespace Runtime.Structure
{
    public class StructureDestructor : IStructureDestructor
    {
        public void Destruct(IStructure structure)
        {
            CycleService.UnregisterStructure(structure);
            foreach (IBlock structureBlock in structure.Blocks)
            {
                DynamicPool.Instance.Return(structureBlock.transform);
            }
            DynamicPool.Instance.Return(structure.transform);
        }
    }
}