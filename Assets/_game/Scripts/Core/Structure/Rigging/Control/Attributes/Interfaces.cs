using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public interface IUpdateDevice
    {
        void UpdateDevice();
    }

    public interface IDevice : IUpdateDevice
    {
        IStructure Structure { get; }
        IBlock Block { get; }
        string Port { get; set; }
        void Init(IStructure structure, IBlock block, string port);
    }

    public interface IArrowDevice : IDevice
    {
        Transform Arrow { get; }
    }
}
