using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public class HydrogenStorageTest : Block, IHydrogenStorage
    {
        public float MaximalVolume => maximalVolume;
        public float CurrentVolume => currentVolume;

        [SerializeField] private float maximalVolume;
        [SerializeField] private float currentVolume;
        public float maximumOutput;

        public Port<float> output;

        public void FuelTick()
        {
            float delta = Mathf.Clamp(maximumOutput - output.Value, -currentVolume, currentVolume);
            output.Value += delta;
            currentVolume -= delta;
        }
    }
}
