using Core.Graph;
using Core.Graph.Wires;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public interface IDeviceWithPort : IDevice, IPortUser, IInteractiveDevice
    {
    }
    public interface IDevice : ITablePrefab
    {
        IGraphHandler Graph { get; }
        IBlock Block { get; }
        void Init(IGraphHandler graph, IBlock block);
        void UpdateDevice();
    }

    public interface IArrowDevice : IDevice
    {
        Transform Arrow { get; }
    }
}
