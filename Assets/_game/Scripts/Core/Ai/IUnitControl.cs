using UnityEngine;

namespace Core.Ai
{
    public enum MovementType { Direct, Strafe }
    public interface IUnitControl
    {
        public void SetTargetVelocity(Vector3 velocity, MovementType movementType);
    }
}