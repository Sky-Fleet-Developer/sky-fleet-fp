using System;
using NUnit.Framework;
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

    #if UNITY_EDITOR
    public static class LodSettingsTests
    {
        [Test(TestOf = typeof(LodSettings))]
        public static void TestLods()
        {
            LodSettings settings = new LodSettings { lods = new []{new LodSettings.LodSample{distance = 10f}, new LodSettings.LodSample{distance = 100f}, new LodSettings.LodSample{distance = 1000f}} };
            settings.Init();
            Assert.True(true);
            Assert.True(settings.GetLodSqr(5f*5) == 0);
            Assert.True(settings.GetLodSqr(50f*50) == 1);
            Assert.True(settings.GetLodSqr(500f*500) == 2);
            Assert.True(settings.GetLodSqr(1500f*1500) == 3);
        }
    }
    #endif
}