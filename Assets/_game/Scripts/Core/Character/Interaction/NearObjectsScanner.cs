using System.Collections.Generic;
using Core.Data;
using UnityEngine;

namespace Core.Character.Interaction
{
    public class NearObjectsScanner
    {
        private int _lastScanFrame;
        private Collider[] _cache;

        public NearObjectsScanner()
        {
            _cache = new Collider[GameData.Data.maxCollidersToScan];
        }
        
        public void Scan(Vector3 position)
        {
            Physics.OverlapSphereNonAlloc(position, GameData.Data.interactionDistance, _cache, GameData.Data.interactiveLayer);
        }

        public void ScanThisFrame(Vector3 position)
        {
            if (_lastScanFrame == Time.frameCount)
            {
                return;
            }
            _lastScanFrame = Time.frameCount; 
            Scan(position);
        }

        public IEnumerable<T> GetResults<T>()
        {
            for (var i = 0; i < _cache.Length; i++)
            {
                if (_cache[i].gameObject.TryGetComponent(out T result))
                {
                    yield return result;
                }
            }
        }
    }
}