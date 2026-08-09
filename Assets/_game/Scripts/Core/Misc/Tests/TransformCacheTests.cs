using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Core.Misc.Tests
{
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