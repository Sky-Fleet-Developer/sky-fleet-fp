using UnityEngine;

namespace Core.Ai
{
    public interface IUnitControl
    {
        public bool IsActive { get; }
        public void SetUpVector(IDirectionData direction);
        public void SetForwardVector(IDirectionData direction);
        public void SetSpeed(float speed);
        public void SetRollYawFactor(float factor);
        public void SetDriftCompensation(float value);
    }
}