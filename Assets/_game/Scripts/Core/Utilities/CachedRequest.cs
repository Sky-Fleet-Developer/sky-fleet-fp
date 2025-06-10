using System;
using UnityEngine;

namespace Core.Utilities
{
    public class CachedRequest<T>
    {
        private T _value;
        private float _lastRequestTime;
        private float _updateThreshold;
        private Func<T> _request;
        
        public CachedRequest(float updateThreshold, Func<T> request)
        {
            _request = request;
            _updateThreshold = updateThreshold;
        }

        public T Value
        {
            get
            {
                if (_lastRequestTime + _updateThreshold < Time.time)
                {
                    _value = _request();
                    _lastRequestTime = Time.time;
                }

                return _value;
            }
        }

        public static implicit operator T(CachedRequest<T> cachedRequest)
        {
            return cachedRequest.Value;
        }
    }

}