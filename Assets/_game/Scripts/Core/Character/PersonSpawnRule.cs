using UnityEngine;

namespace Core.Character
{
    public abstract class PersonSpawnRule : MonoBehaviour
    {
        public abstract bool TryGetSpawnPoint(out Vector3 point);
    }
}