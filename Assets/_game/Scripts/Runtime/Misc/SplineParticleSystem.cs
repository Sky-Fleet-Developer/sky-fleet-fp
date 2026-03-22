using System.Collections.Generic;
using Core;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Runtime.Misc
{
    public class SpsPoint
    {
        public int Index;
        public float Time;
        public Vector2 SideOffset;
        public SpsPoint(int index) => Index = index;
    }
    
    public class SplineParticleSystem : MonoBehaviour
    {
        public enum LoopMode
        {
            Clamp = 0,
            Loop = 1,
            PingPong = 2
        }
        [SerializeField] private bool debugDraw;
        [SerializeField] private int baseCount = 1;
        [SerializeField] private float densityOverDistance = 0.01f;
        [SerializeField] private float longitudeIrregularity = 0.5f;
        [SerializeField] private float clusterCenterIrregularity = 0.5f;
        [SerializeField] private float timeOffset;
        [SerializeField][Tooltip("Meters per second")] private float speed;
        [SerializeField] private int2 clusterization;
        [SerializeField] private float shapeSize;
        [SerializeField] private LoopMode loopMode;
        [SerializeField, ShowIf("@loopMode == LoopMode.PingPong")] private bool flipHorizontalOffsetInReverseDirection;
        [SerializeField] private int seed;
        [ShowInInspector, ReadOnly] private Bounds _bounds;
        private List<SpsPoint> _points;
        private SplineContainer _spline;
        private float _splineLength;
        private System.Random _random;
        private int _splineHash;
        private float _speedNormalized;
        
        public int PointCount => _points.Count;
        public IReadOnlyList<SpsPoint> Points => _points;
        public SpsPoint GetPoint(int index) => _points[index];
        public Bounds LocalBounds => _bounds;

        private void OnValidate()
        {
            EnsureObjects();
            FillPoints();
            CalculateBounds();
            _speedNormalized = speed / _splineLength;
        }

        private void EnsureObjects()
        {
            _spline ??= GetComponent<SplineContainer>();
            _splineHash = 0;
            foreach (var splineSpline in _spline.Splines)
            {
                foreach (var splineSplineKnot in splineSpline.Knots)
                {
                    _splineHash += splineSplineKnot.GetHashCode();
                }
            }
        }
        
        private void Awake()
        {
            EnsureObjects();
            FillPoints();
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            _bounds.size = Vector3.zero;
            if (_spline.Splines.Count == 0 || _spline.Splines[0].Count == 0)
            {
                _bounds.center = Vector3.zero;
                return;
            }
            _bounds.center = _spline.Splines[0][0].Position;
            for (int i = 1; i < 100; i++)
            {
                float t = i / 99f;
                _bounds.Encapsulate(transform.InverseTransformPoint(_spline.EvaluatePosition(t)));
            }
        }
        
        private void FillPoints()
        {
            _splineLength = _spline.CalculateLength();
            float pointCountF = baseCount + _splineLength * densityOverDistance;
            if (loopMode == LoopMode.PingPong)
            {
                pointCountF *= 2;
            }
            int pointCount = Mathf.CeilToInt(pointCountF);
            _points ??= new(pointCount);
            if (_points.Count > pointCount)
            {
                _points.RemoveRange(pointCount, _points.Count - pointCount);
            }
            else
            {
                while (_points.Count < pointCount)
                {
                    _points.Add(new SpsPoint(_points.Count));
                }
            }
            
            _random = new (seed);
            float gap = _splineLength / pointCountF;
            int clusterSize = _random.Next(clusterization.x, clusterization.y);
            int clusterCounter = clusterSize;
            int clusterCenter = clusterSize / 2;
            float clusterCenterOffset = (float)_random.NextDouble() * clusterCenterIrregularity * clusterSize * gap;
            for (int i = 0; i < _points.Count; i++)
            {
                int index = i;
                if (clusterSize > 0)
                {
                    if (--clusterCounter == 0)
                    {
                        clusterSize = _random.Next(clusterization.x, clusterization.y);
                        clusterCounter = clusterSize;
                        clusterCenter = i + clusterSize / 2;
                        clusterCenterOffset = (float)_random.NextDouble() * clusterCenterIrregularity * clusterSize * gap;
                    }

                    index = clusterCenter;
                }

                _points[i].SideOffset = new Vector2((float)_random.NextDouble() * 2 - 1, (float)_random.NextDouble() * 2 - 1);
                _points[i].Time = clusterCenterOffset + index * gap + ((float)_random.NextDouble() - 0.5f * 2) * gap * longitudeIrregularity;
                if (loopMode == LoopMode.PingPong)
                {
                    _points[i].Time *= 2;
                }
            }
        }

        public void EvaluatePoint(int pointIndex, out Vector3 position, out Quaternion rotation, out bool isReverse, out int lap)
        {
            SpsPoint p = _points[pointIndex];
            EvaluatePoint(p, out position, out rotation, out isReverse, out lap);
        }

        public void EvaluatePoint(SpsPoint point, out Vector3 position, out Quaternion rotation, out bool isReverse, out int lap)
        {
            float t = point.Time / _splineLength + timeOffset + _speedNormalized * Time.time;
            isReverse = false;
            lap = 0;
            switch (loopMode)
            {
                case LoopMode.Clamp:
                    t = Mathf.Clamp01(t);
                    break;
                case LoopMode.Loop:
                    lap = Mathf.FloorToInt(t);
                    t = Mathf.Repeat(t, 1);
                    break;
                case LoopMode.PingPong:
                    lap = Mathf.FloorToInt(t / 2);
                    isReverse = Mathf.FloorToInt(t) % 2 == 1;
                    t = Mathf.PingPong(t, 1);
                    break;
            }
            _spline.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
            rotation = Quaternion.LookRotation(isReverse ? -tan : tan, up);
            position = (Vector3)pos + rotation * new Vector3(point.SideOffset.x * shapeSize * (isReverse && flipHorizontalOffsetInReverseDirection ? -1 : 1), point.SideOffset.y * shapeSize, 0);
        }
        

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!debugDraw)
            {
                return;
            }
            var prevHash = _splineHash;
            EnsureObjects();
            if (prevHash != _splineHash)
            {
                FillPoints();
            }
            
            var mat = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            foreach (var splineSpline in _spline.Splines)
            {
                foreach (var bezierKnot in splineSpline)
                {
                    Handles.CircleHandleCap(0, bezierKnot.Position, bezierKnot.Rotation, shapeSize, EventType.Repaint);
                }
            }
            Handles.matrix = mat;
            foreach (var spawnPoint in _points)
            {
                EvaluatePoint(spawnPoint, out Vector3 pos, out Quaternion rot, out _, out _);
                Handles.ArrowHandleCap(0, pos, rot, 15 + HandleUtility.GetHandleSize(pos) * 0.2f, EventType.Repaint);
            }
        }
#endif
    }
}