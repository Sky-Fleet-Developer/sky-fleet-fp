
using System.Collections.Generic;
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
            _toleranceBounds = new Bounds((Vector3)_cell * _size, Vector3.one * (_size + _tolerance));
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

        public IEnumerable<Vector3Int> IntersectRay(Ray ray, float distance)
        {
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;

            // Текущая ячейка (начальная точка)
            Vector3Int currentCell = PositionToCell(origin);

            // Направление шага по осям (1 или -1)
            int stepX = direction.x >= 0 ? 1 : -1;
            int stepY = direction.y >= 0 ? 1 : -1;
            int stepZ = direction.z >= 0 ? 1 : -1;

            // Расстояние, которое нужно пройти по лучу, чтобы пересечь одну целую ячейку по каждой оси
            float tDeltaX = Mathf.Abs(_size / direction.x);
            float tDeltaY = Mathf.Abs(_size / direction.y);
            float tDeltaZ = Mathf.Abs(_size / direction.z);

            // Находим границы текущей ячейки. 
            // Так как вы используете RoundToInt, граница ячейки — это (Index +/- 0.5) * cellSize
            float boundaryX = (currentCell.x + stepX * 0.5f) * _size;
            float boundaryY = (currentCell.y + stepY * 0.5f) * _size;
            float boundaryZ = (currentCell.z + stepZ * 0.5f) * _size;

            // tMax — это значение параметра t, при котором луч пересечет первую границу ячейки
            float tMaxX = (direction.x != 0) ? (boundaryX - origin.x) / direction.x : float.PositiveInfinity;
            float tMaxY = (direction.y != 0) ? (boundaryY - origin.y) / direction.y : float.PositiveInfinity;
            float tMaxZ = (direction.z != 0) ? (boundaryZ - origin.z) / direction.z : float.PositiveInfinity;

            yield return currentCell;

            float traveled = 0;

            while (true)
            {
                // Выбираем ось, по которой пересечение произойдет быстрее всего
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        traveled = tMaxX;
                        if (traveled > distance) break;
                        currentCell.x += stepX;
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        traveled = tMaxZ;
                        if (traveled > distance) break;
                        currentCell.z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        traveled = tMaxY;
                        if (traveled > distance) break;
                        currentCell.y += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        traveled = tMaxZ;
                        if (traveled > distance) break;
                        currentCell.z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }

                yield return currentCell;
            }
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