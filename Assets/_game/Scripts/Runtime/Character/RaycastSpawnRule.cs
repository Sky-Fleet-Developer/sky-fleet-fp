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
        [Inject(Optional = true)] private TerrainProvider _terrainProvider;
        [Inject] private LocationChunksSet _locationChunksSet; 
        [SerializeField]
        private bool allowSpawnWithoutHit;
        private RaycastHit[] _results = new RaycastHit[10];
        
        public override bool TryGetSpawnPoint(out Vector3 point)
        {
            point = transform.position;
            if (_terrainProvider && !_terrainProvider.IsDeformersClear() && (_locationChunksSet.SetRangeTask == null || _locationChunksSet.SetRangeTask.IsCompleted))
            {
                return false;
            }

            var size = Physics.RaycastNonAlloc(transform.position + Vector3.up * 10000, Vector3.down, _results, 11000, GameData.Data.walkableLayer);

            if (size > 0)
            {
                int closest = -1;
                float distance = float.MaxValue;
                for (int i = 0; i < size; i++)
                {
                    float d = Mathf.Abs(transform.position.y - _results[i].point.y);
                    if (d < distance)
                    {
                        closest = i;
                        distance = d;
                    }
                }

                point = _results[closest].point + Vector3.up;
                return true;
            }


            return allowSpawnWithoutHit;
        }
    }
}