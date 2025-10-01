
using UnityEngine;

namespace Core.World
{
    public struct Grid
    {
        private readonly float _size;
        private readonly bool _useY;
        private readonly float _sizeInv;
        private Vector3Int _cell;
        private float _tolerance;
        private Bounds _toleranceBounds;

        public float Size => _size;
        
        public Grid(Vector3 startPosition, float cellSize, bool useY) : this()
        {
            _size = cellSize;
            _useY = useY;
            _sizeInv = 1 / _size;
            _tolerance = 0;
            _toleranceBounds = default;
            _cell = PositionToCell(startPosition);
        }

        public void SetBorderTolerance(float value)
        {
            _tolerance = value;
        }
        
        public bool Update(Vector3 position, out Vector3Int cell)
        {
            cell = PositionToCell(position);
            if (cell != _cell)
            {
                if (_tolerance > 0)
                {
                    if (!_useY)
                    {
                        position.y = 0;
                    }
                    if (_toleranceBounds.Contains(position))
                    {
                        return false;
                    }
                }

                _cell = cell;
                _toleranceBounds = new Bounds((Vector3)_cell * _size, Vector3.one * (_size + _tolerance));
                return true;
            }
            return false;
        }

        public int PositionToCell(float value)
        {
            return Mathf.RoundToInt(value * _sizeInv);
        }
        public Vector3Int PositionToCell(Vector3 value)
        {
            return new Vector3Int(PositionToCell(value.x), _useY ? PositionToCell(value.y) : 0, PositionToCell(value.z));
        }

        public int GetDistance(Vector3Int point)
        {
            return Mathf.Max(Mathf.Abs(point.x - _cell.x), Mathf.Abs(point.y - _cell.y), Mathf.Abs(point.z - _cell.z));
        }
    }
}