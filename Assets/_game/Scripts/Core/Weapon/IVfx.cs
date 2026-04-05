using UnityEngine;

namespace Core.Weapon
{
    public interface IVfx
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public bool NeedManualReturn { get; }
        public void Play();
        public void Stop();
        // ReSharper disable once InconsistentNaming
        public Transform transform { get; }
    }
}