using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public interface IDevice
    {
        IStructure Structure { get; }
        IBlock Block { get; }
        void Init(IStructure structure, IBlock block);
        void UpdateDevice();

    }

    public interface IArrowDevice : IDevice
    {
        Transform Arrow { get; }
    }
}
