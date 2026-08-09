using System;
using UnityEngine;

namespace Core.Utilities
{
    [Serializable]
    public class LodSettings
    {
        [Serializable]
        public class LodSample
        {
            public float distance;
            public int refreshPeriod;
        }

        public LodSample[] lods;
        public int hiddenLodRefreshPeriod;
        private float[] _sqrLods;

        public float GetMaxLodDistance()
        {
            return lods[^1].distance;
        }

        public float GetLodDistance(int lod)
        {
            if (lod >= lods.Length)
            {
                return Mathf.Infinity;
            }

            return lods[lod].distance;
        }

        public int GetLodRefreshPeriod(int lod)
        {
            if (lod >= lods.Length)
            {
                return hiddenLodRefreshPeriod;
            }

            return lods[lod].refreshPeriod;
        }

        public void Init()
        {
            _sqrLods = new float[lods.Length];
            for (var i = 0; i < _sqrLods.Length; i++)
            {
                _sqrLods[i] = lods[i].distance * lods[i].distance;
            }
        }

        public int GetLodSqr(float sqrDistance)
        {
            for (int i = 0; i < _sqrLods.Length; i++)
            {
                if (sqrDistance <= _sqrLods[i])
                {
                    return i;
                }
            }

            return _sqrLods.Length;
        }
    }
}