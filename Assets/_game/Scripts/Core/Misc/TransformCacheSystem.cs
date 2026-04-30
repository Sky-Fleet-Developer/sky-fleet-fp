using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.World;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Debug = UnityEngine.Debug;

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
        //private List<int> _sparse = new();
        //private List<int> _dense = new();
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

    [TestFixture(TestOf = typeof(TransformCacheSystem))]
    public static class TransformCacheTests
    {
        [Test]
        [TestCase(1000, 100)]
        [TestCase(10000, 100)]
        public static void Test(int transformsAmount, int iterations)
        {
            TransformCacheSystem system = new();
            Transform[] transforms = new Transform[transformsAmount];
            for (int i = 0; i < transformsAmount; i++)
            {
                transforms[i] = new GameObject($"TestTransform {i}").transform;
                transforms[i].position = new Vector3(i, i, i);
                transforms[i].rotation = Quaternion.Euler(i, i, i);
                system.AddTarget(transforms[i]);
            }

            JobHandle handle = default;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                handle = system.Update(handle);
            }

            handle.Complete();
            stopwatch.Stop();
            float updateTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            for (int i = 0; i < transformsAmount; i++)
            {
                var data = system.Read(transforms[i]);
            }

            stopwatch.Stop();
            float readTime = stopwatch.ElapsedMilliseconds;

            for (int i = 1; i < transformsAmount; i++)
            {
                Assert.AreEqual(transforms[i].position, system.Read(transforms[i]).Position);
                Assert.AreEqual(transforms[i].rotation, system.Read(transforms[i]).Rotation);
            }
            
            Assert.Throws<KeyNotFoundException>(() => system.Read(transforms[0]));

            Debug.Log($"Update time: ({iterations} iterations, {transformsAmount} transforms): {updateTime} ms");
            Debug.Log($"Read time: ({transformsAmount} transforms): {readTime} ms");

            system.Dispose();
        }
        
        [Test]
        public static void TestRemove()
        {
            int transformsAmount = 10;
            TransformCacheSystem system = new();
            Transform[] transforms = new Transform[10];
            for (int i = 0; i < transformsAmount; i++)
            {
                transforms[i] = new GameObject($"TestTransform {i}").transform;
                transforms[i].position = new Vector3(i, i, i);
                transforms[i].rotation = Quaternion.Euler(i, i, i);
                system.AddTarget(transforms[i]);
            }
            system.RemoveTarget(transforms[0]);

            system.Update().Complete();

            for (int i = 0; i < transformsAmount - 1; i++)
            {
                Assert.AreEqual(transforms[i].position, system.Read(transforms[i]).Position);
                Assert.AreEqual(transforms[i].rotation, system.Read(transforms[i]).Rotation);
            }

            system.Dispose();
        }

#if UNITY_EDITOR
        [Test]
        [TestCase(1000, 100)]
        [TestCase(10000, 100)]
        public static void TestMainThread(int transformsAmount, int iterations)
        {
            TransformCacheSystem system = new();
            Transform[] transforms = new Transform[transformsAmount];
            for (int i = 0; i < transformsAmount; i++)
            {
                transforms[i] = new GameObject($"TestTransform {i}").transform;
                transforms[i].position = new Vector3(i, i, i);
                transforms[i].rotation = Quaternion.Euler(i, i, i);
                system.AddTarget(transforms[i]);
            }

            Stopwatch stopwatch = new();
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                system.UpdateInMainThread();
            }

            stopwatch.Stop();
            float updateTime = stopwatch.ElapsedMilliseconds;

            for (int i = 0; i < transformsAmount; i++)
            {
                Assert.AreEqual(transforms[i].position, system.ReadFromTest(transforms[i]).Position);
                Assert.AreEqual(transforms[i].rotation, system.ReadFromTest(transforms[i]).Rotation);
            }

            Debug.Log($"Update time: ({iterations} iterations, {transformsAmount} transforms): {updateTime} ms");
            system.Dispose();
        }
#endif
    }
}