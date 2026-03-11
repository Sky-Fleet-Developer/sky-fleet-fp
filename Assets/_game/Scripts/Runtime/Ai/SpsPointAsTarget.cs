using Core.Ai;
using Runtime.Misc;
using UnityEngine;

namespace Runtime.Ai
{
    public class SpsPointAsTarget : ITargetData
    {
        public Vector3 Position
        {
            get
            {
                if (!IsValid())
                {
                    UpdateRecord();
                }
                return _positionCache;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (!IsValid())
                {
                    UpdateRecord();
                }
                return _rotationCache;
            }
        }

        public Vector3 Velocity
        {
            get
            {
                if (!IsValid())
                {
                    UpdateRecord();
                }
                return _velocityCache;
            }
        }
        
        private Vector3 _positionCache;
        private Quaternion _rotationCache;
        private Vector3 _velocityCache;
        private int _prevDetectionFrame;
        private float _prevDetectionTime;
        
        private int _splinePointIndex;
        private SplineParticleSystem _spline;

        public SpsPointAsTarget(int splinePointIndex, SplineParticleSystem spline)
        {
            _splinePointIndex = splinePointIndex;
            _spline = spline;
            UpdateRecord();
        }
        
        private bool IsValid()
        {
            return Time.frameCount == _prevDetectionFrame;
        }
        private void UpdateRecord()
        {
            _prevDetectionFrame = Time.frameCount;
            Vector3 prevPos = _positionCache;
            _spline.EvaluatePoint(_splinePointIndex, out _positionCache, out _rotationCache, out _, out _);
            float dt = Time.time - _prevDetectionTime;
            if (dt < 0.01f)
            {
                _velocityCache = (_positionCache - prevPos) / dt;
            }
            _prevDetectionTime = Time.time;
        }
    }
}