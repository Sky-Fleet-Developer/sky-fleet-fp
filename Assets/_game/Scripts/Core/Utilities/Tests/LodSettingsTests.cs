using NUnit.Framework;

namespace Core.Utilities.Tests
{
    public static class LodSettingsTests
    {
        [Test(TestOf = typeof(LodSettings))]
        public static void TestLods()
        {
            LodSettings settings = new LodSettings
            {
                lods = new[]
                {
                    new LodSettings.LodSample { distance = 10f }, new LodSettings.LodSample { distance = 100f },
                    new LodSettings.LodSample { distance = 1000f }
                }
            };
            settings.Init();
            Assert.True(true);
            Assert.True(settings.GetLodSqr(5f * 5) == 0);
            Assert.True(settings.GetLodSqr(50f * 50) == 1);
            Assert.True(settings.GetLodSqr(500f * 500) == 2);
            Assert.True(settings.GetLodSqr(1500f * 1500) == 3);
        }
    }
}