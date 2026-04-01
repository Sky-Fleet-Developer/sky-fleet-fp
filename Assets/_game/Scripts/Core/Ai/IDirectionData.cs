using UnityEngine;

namespace Core.Ai
{
    public interface IDirectionData
    {
        /// <returns>Raw, non-normalized vector</returns>
        public Vector3 GetDirection(Vector3 origin);
        /// <returns>Raw, non-normalized vector</returns>
        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time);
    }

    public class SmoothTurn : IDirectionData
    {
        public Quaternion Value;
        public ITargetData Self;

        public Vector3 GetDirection(Vector3 origin)
        {
            return Value * Self.Velocity;
        }

        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time)
        {
            return GetDirection(origin);
        }
    }

    public class ConstantDirection : IDirectionData
    {
        public Vector3 Direction { get; set; }
        public ConstantDirection(Vector3 direction) => Direction = direction;
        public Vector3 GetDirection(Vector3 origin) => Direction;
        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time) => Direction;
    }

    public class MatchTargetUp : IDirectionData
    {
        private ITargetData _targetData;

        public MatchTargetUp(ITargetData targetData)
        {
            _targetData = targetData;
        }

        public Vector3 GetDirection(Vector3 origin)
        {
            return _targetData.Rotation * Vector3.up;
        }

        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time)
        {
            return GetDirection(origin);
        }
    }
}