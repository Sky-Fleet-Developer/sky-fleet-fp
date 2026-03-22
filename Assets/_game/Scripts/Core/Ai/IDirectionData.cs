using UnityEngine;

namespace Core.Ai
{
    public interface IDirectionData
    {
        public Vector3 GetDirection(Vector3 origin);
        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time);
    }

    public class ConstantDirection : IDirectionData
    {
        public Vector3 Direction { get; set; }
        public ConstantDirection(Vector3 direction) => Direction = direction;
        public Vector3 GetDirection(Vector3 origin) => Direction;
        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time) => Direction;
    }
}