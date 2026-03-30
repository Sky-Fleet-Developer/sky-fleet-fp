using UnityEngine;

namespace Core.Ai
{
    public interface IUnitControl
    {
        public bool IsActive { get; }
        public void SetUpVector(IDirectionData direction);
        public void SetForwardDirection(IDirectionData direction);
        public void SetPredictionTime(float time);
        public void SetSpeed(float speed);
        public void SetRollYawFactor(float factor);
        public void SetRollBackFactor(float factor);
        public void SetDriftCompensation(float value);
    }
}