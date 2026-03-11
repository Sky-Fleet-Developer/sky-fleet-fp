using UnityEngine;

namespace Core.Ai
{
    public interface IDirectionData
    {
        public Vector3 GetDirection(Vector3 origin);
    }

    public class ConstantDirection : IDirectionData
    {
        public Vector3 Direction { get; set; }
        public ConstantDirection(Vector3 direction) => Direction = direction;
        public Vector3 GetDirection(Vector3 origin) => Direction;
    }
}