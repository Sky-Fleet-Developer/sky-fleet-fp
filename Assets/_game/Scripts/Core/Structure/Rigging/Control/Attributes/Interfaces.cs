using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public interface IDevice
    {
        IStructure Structure { get; }
        IBlock Block { get; }
        string Port { get; set; }
        void Init(IStructure structure, IBlock block, string port);
        void UpdateDevice();
    }

    public interface IArrowDevice : IDevice
    {
        Transform Arrow { get; }
    }
}
