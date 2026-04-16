using UnityEngine;

namespace Core.Weapon
{
    public class ProjectileInstance
    {
        private const float G = 9.8f;
        public int Id { get; }
        public ShellData ShellData { get; }
        public float InitialTime { get; }
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector3 Velocity => _velocity;
        public Vector3 PreviousPosition => _previousPosition;

        private Vector3 _velocity;
        private Vector3 _position;
        private Vector3 _previousPosition;

        public ProjectileInstance(Vector3 origin, Vector3 nonUnitDirection, Vector3 initialVelocity, float speed, ShellData shellData, int id)
        {
            ShellData = shellData;
            Id = id;
            InitialTime = Time.time;
            _position = origin;
            _previousPosition = origin;
            _velocity = initialVelocity + nonUnitDirection.normalized * speed;
        }

        public void Step(float fixedDeltaTime)
        {
            _velocity += Vector3.down * (G * fixedDeltaTime);
            //_velocity *= (1 - Config.data.airDrag * fixedDeltaTime);
            _previousPosition = _position;
            _position += _velocity * fixedDeltaTime;
            Debug.DrawLine(_previousPosition, _position, Color.yellow, 2);
        }

        public void Reflect(Vector3 normal)
        {
            _velocity = Vector3.Reflect(_velocity, normal) * 0.6f;
        }

        public void Dispose()
        {
            
        }
    }
}