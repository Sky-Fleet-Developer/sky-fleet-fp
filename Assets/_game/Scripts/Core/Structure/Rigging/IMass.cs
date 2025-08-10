using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IMass
    {
        float Mass { get; }
        Vector3 LocalCenterOfMass { get; }
    }
}