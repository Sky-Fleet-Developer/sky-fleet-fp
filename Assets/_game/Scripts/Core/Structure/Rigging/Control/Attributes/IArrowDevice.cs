using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{
    public interface IArrowDevice : IDevice
    {
        Transform Arrow { get; }
    }
}