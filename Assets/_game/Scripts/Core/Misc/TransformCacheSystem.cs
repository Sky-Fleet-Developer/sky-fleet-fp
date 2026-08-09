using System;
using System.Collections.Generic;
using Core.World;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Core.Misc
{
    public struct TransformCache
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class TransformCacheSystem : IDisposable, ITickable
    {
        private NativeList<TransformCache> _caches = new(32, Allocator.Persistent);
        private TransformAccessArray _transforms = new(32);
        private Dictionary<Transform, int> _transformMap = new();
#if UNITY_EDITOR
        private Dictionary<Transform, TransformCache> _testCache = new();
#endif
        public int TickRate => 1;
        static TransformCacheSystem()
        {
            TickService.SetUpdate(typeof(TransformCacheSystem), false);
            TickService.SetOrderBefore(typeof(TransformCacheSystem), typeof(WorldGrid));
        }
        public void AddTarget(Transform transform)
        {
            _transformMap.Add(transform, _transforms.length);
            _transforms.Add(transform);
            _caches.Add(default);
#if UNITY_EDITOR
            _testCache[transform] = default;
#endif
        }

        public void RemoveTarget(Transform transform)
        {
            // find indices 
            int source = _transformMap[transform];
            int last = _transforms.length - 1;
            if (last != source)
            {
                // swap last element to the middle
                _transformMap[_transforms[last]] = source;
            }
            
            _transforms.RemoveAtSwapBack(last);
            _caches.RemoveAt(last);
#if UNITY_EDITOR
            _testCache.Remove(transform);
#endif
        }

        public TransformCache Read(Transform transform) => _caches[_transformMap[transform]];
#if UNITY_EDITOR
        public TransformCache ReadFromTest(Transform transform) => _testCache[transform];
#endif

        public void Tick()
        {
            Update().Complete();
        }

        public JobHandle Update(JobHandle dependency = default)
        {
            return new MyJob { Caches = _caches }.ScheduleReadOnly(_transforms, 32, dependency);
        }

#if UNITY_EDITOR
        public void UpdateInMainThread()
        {
            for (int i = 0; i < _transforms.length; i++)
            {
                var transform = _transforms[i];
                _testCache[transform] = new TransformCache
                    { Position = transform.position, Rotation = transform.rotation };
            }
        }
#endif

        private struct MyJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction] public NativeList<TransformCache> Caches;

            public void Execute(int index, TransformAccess transform)
            {
                Caches[index] = new TransformCache { Position = transform.position, Rotation = transform.rotation };
            }
        }

        public void Dispose()
        {
            _caches.Dispose();
            _transforms.Dispose();
        }
    }
}