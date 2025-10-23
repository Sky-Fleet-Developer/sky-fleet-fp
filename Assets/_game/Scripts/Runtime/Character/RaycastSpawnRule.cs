using Core.Character;
using Core.Data;
using Core.TerrainGenerator;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Character
{
    public class RaycastSpawnRule : PersonSpawnRule
    {
        [Inject] private TerrainProvider _terrainProvider;
        [Inject] private LocationChunksSet _locationChunksSet; 
        [SerializeField]
        private bool allowSpawnWithoutHit;
        public override bool TryGetSpawnPoint(out Vector3 point)
        {
            point = transform.position;
            if (!_terrainProvider.IsDeformersClear() && (_locationChunksSet.SetRangeTask == null || _locationChunksSet.SetRangeTask.IsCompleted))
            {
                return false;
            }

            if (Physics.Raycast(transform.position + Vector3.up * 10000, Vector3.down, out RaycastHit groundHit, 11000, GameData.Data.walkableLayer))
            {
                point = groundHit.point + Vector3.up;
                return true;
            }

            return allowSpawnWithoutHit;
        }
    }
}