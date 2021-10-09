using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class SmallBattery : Block, IPowerUser
    {
        public float maxInput;
        public float maxOutput;

        public Port<float> storage = new Port<float>(PortType.Power);

        private float storagePower;

        public void PowerTick()
        {
            float delta = Mathf.Clamp(maxOutput - storage.Value, maxOutput, maxInput);
            storagePower += delta;
            storage.Value -= delta;
        }
    }
}