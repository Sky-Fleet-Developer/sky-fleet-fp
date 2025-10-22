using Core.World;
using UnityEngine;

namespace WorldEditor
{
    public class DynamicPositionFromWorldRect : IDynamicPositionProvider
    {
        private WorldGrid _grid;
        private LocationChunksSet _locationChunksSet;

        public DynamicPositionFromWorldRect(WorldGrid grid, LocationChunksSet locationChunksSet)
        {
            _locationChunksSet = locationChunksSet;
            _grid = grid;
        }

        public Vector3 WorldPosition
        {
            get
            {
                var center = _locationChunksSet.GetRange().center;
                return new Vector3(center.x, 0, center.y) * _grid.GetCellSize();
            }
        }
        public Vector3 SpacePosition => WorldPosition - WorldOffset.Offset;
  
        public Vector3 StoredVelocity => Vector3.zero;
        public Vector3 GetPredictedWorldPosition(float time)
        {
            return WorldPosition;
        }
    }
}