using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    public class TransformTracker : MonoBehaviour, IDynamicPositionProvider
    {
        [SerializeField] private int historyLength = 50; 
        [SerializeField] private float cachePositionDelay = 1f;
        [SerializeField] private float minimalCacheDistance = 50;
        private float _previousCacheTime;
        private Vector3[] _history;
        private float[] _historyTimeMarks;
        private Vector3 _storedVelocity;
        private int _historyPointer;
        [ShowInInspector] public Vector3 StoredVelocity => _storedVelocity;
        [ShowInInspector] public Vector3 WorldPosition => _isInitialized ? _history[_historyPointer] : Vector3.zero;
        public Vector3 SpacePosition => _isInitialized ? _history[_historyPointer] : Vector3.zero;
        
        public Vector3 GetPredictedWorldPosition(float time) => WorldPosition + _storedVelocity * time;
        private bool _isInitialized;
        public void Start()
        {
            _history = new Vector3[historyLength];
            _historyTimeMarks = new float[historyLength];
            _isInitialized = true;
            for (var i = 0; i < _history.Length; i++)
            {
                WriteHistory();
            }
            _historyTimeMarks[_historyPointer] += 1;
            CalculateStoredVelocity();
        }

        private void Update()
        {
            if (_previousCacheTime + cachePositionDelay < Time.time && IsDistanceEnough())
            {
                _previousCacheTime = Time.time;
                WriteHistory();
                CalculateStoredVelocity();
            }
        }

        private bool IsDistanceEnough()
        {
            return Vector3.SqrMagnitude(transform.position - _history[_historyPointer]) >
                   minimalCacheDistance * minimalCacheDistance;
        }
        
        private void WriteHistory()
        {
            _historyPointer++;
            _historyPointer %= historyLength;
            _history[_historyPointer] = transform.position - WorldOffset.Offset;
            _historyTimeMarks[_historyPointer] = Time.time;
        }

        private void CalculateStoredVelocity()
        {
            int prev = _historyPointer == 0 ? historyLength - 1 : _historyPointer - 1;
            _storedVelocity = (_history[_historyPointer] - _history[prev]) / (_historyTimeMarks[_historyPointer] - _historyTimeMarks[prev]);
        }
    }
}