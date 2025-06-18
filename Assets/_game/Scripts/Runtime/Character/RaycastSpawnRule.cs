using Core.Character;
using Core.Data;
using UnityEngine;

namespace Runtime.Character
{
    public class RaycastSpawnRule : PersonSpawnRule
    {
        [SerializeField]
        private bool allowSpawnWithoutHit;
        public override bool TryGetSpawnPoint(out Vector3 point)
        {
            point = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 10000, Vector3.down, out RaycastHit groundHit, 11000, GameData.Data.walkableLayer))
            {
                point = groundHit.point + Vector3.up;
                return true;
            }

            return allowSpawnWithoutHit;
        }
    }
}