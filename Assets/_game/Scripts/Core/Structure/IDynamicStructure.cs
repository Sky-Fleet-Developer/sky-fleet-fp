using Core.Structure.Rigging;
using UnityEngine;

namespace Core.Structure
{
    public interface IDynamicStructure : IStructure, IMass
    {
        float TotalMass { get; }
        Vector3 LocalCenterOfMass { get; }
        Vector3 Velocity { get; }

        Rigidbody Physics { get; }

        Vector3 GetVelocityForPoint(Vector3 worldPoint);
        void RecalculateMass();
        void AddForce(Vector3 force, Vector3 position);
    }
}