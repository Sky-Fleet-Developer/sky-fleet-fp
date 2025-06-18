using System;
using UnityEngine;

namespace Core.Utilities
{
    public class CachedRequest<T>
    {
        private T _value;
        private float _prevRequestTime;
        private float _updateThreshold;
        private Func<T> _request;
        
        public CachedRequest(float updateThreshold, Func<T> request)
        {
            _request = request;
            _updateThreshold = updateThreshold;
            _prevRequestTime = -updateThreshold * 2;
        }

        public T Value
        {
            get
            {
                if (_prevRequestTime + _updateThreshold < Time.time)
                {
                    _value = _request();
                    _prevRequestTime = Time.time;
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