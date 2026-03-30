using System;
using System.Collections.Generic;
using Core;
using Core.Data;
using Core.World;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Zenject;

namespace Runtime.Misc
{
    public interface ISpsListener
    {
        void OnPointCompleteLap(int index, SpsPoint point);
        void OnPointChangeDirection(int index, SpsPoint point);
        void OnPointDisabled(int index, SpsPoint point);
        void OnPointEnabled(int index, SpsPoint point);
    }

    [RequireComponent(typeof(SplineParticleSystem))]
    public class SpsViewRange : MonoBehaviour
    {
        private SplineParticleSystem _spline;
        private bool[] _pointsViewData;
        private List<SpsPointData> _viewedPoints = new();
        private int _viewedPointsCount;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;
        private int _prevUpdateTick;
        private bool _isBoundsInView;
        private float _viewRangeSqr;
        
        public SplineParticleSystem Spline => _spline;
        
        public event Action<SpsPoint> OnPointBecameVisible;
        public event Action<SpsPoint> OnPointBecameInvisible;

        public class SpsPointData
        {
            public SpsPoint Point;
            public Vector3 Position;
            public Quaternion Rotation;
        }
        private void EnsureObjects()
        {
            _spline ??= GetComponent<SplineParticleSystem>();
            if (_pointsViewData == null || _pointsViewData.Length != _spline.Points.Count)
            {
                _pointsViewData = new bool[_spline.Points.Count];
            }
        }

        public void SetViewRange(float range)
        {
            _viewRangeSqr = range*range;
        }

        private void Awake()
        {
            EnsureObjects();
            gameObject.AddWorldOffsetAnchor();
            Bootstrapper.OnLoadComplete.Subscribe(OnLoadComplete);
            enabled = false;
        }

        private void OnLoadComplete()
        {
            enabled = true;
            //_playerTracker
        }
        
        private void FixedUpdate()
        {
            int targetRefreshTick = _prevUpdateTick + 10;
            if (!_isBoundsInView)
            {
                targetRefreshTick = _prevUpdateTick + 50;
            }

            if (Time.frameCount >= targetRefreshTick)
            {
                Profiler.BeginSample("SpsViewRange");
                _prevUpdateTick = Time.frameCount;
                Vector3 playerPosRelativeToMe = transform.InverseTransformPoint(_playerTracker.SpacePosition);
                float sqrDistance = _spline.LocalBounds.SqrDistance(playerPosRelativeToMe);
                _isBoundsInView = sqrDistance < _viewRangeSqr;

                if (_isBoundsInView)
                {
                    _viewedPointsCount = 0;
                    for (var i = 0; i < _spline.Points.Count; i++)
                    {
                        var p = _spline.GetPoint(i);
                        _spline.EvaluatePoint(p, out Vector3 position, out Quaternion rotation, out _, out _);
                        bool prevValue = _pointsViewData[i];
                        _pointsViewData[i] = Vector3.SqrMagnitude(position - _playerTracker.SpacePosition) < _viewRangeSqr;
                        if (_pointsViewData[i])
                        {
                            if (_viewedPointsCount >= _viewedPoints.Count)
                            {
                                _viewedPoints.Add(new SpsPointData(){Point = p, Position = position, Rotation = rotation});
                            }
                            else
                            {
                                _viewedPoints[_viewedPointsCount].Position = position;
                                _viewedPoints[_viewedPointsCount].Rotation = rotation;
                            }
                            _viewedPointsCount++;
                        }

                        if (prevValue != _pointsViewData[i])
                        {
                            if (_pointsViewData[i])
                            {
                                OnPointBecameVisible?.Invoke(p);
                            }
                            else
                            {
                                OnPointBecameInvisible?.Invoke(p);
                            }
                        }
                    }
                }
                Profiler.EndSample();
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            for (var i = 0; i < _viewedPointsCount; i++)
            {
                Gizmos.DrawSphere(_viewedPoints[i].Position, 1 + HandleUtility.GetHandleSize(_viewedPoints[i].Position) * 0.1f);
            }
        }
        #endif
    }
}