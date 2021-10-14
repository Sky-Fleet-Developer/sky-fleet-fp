using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;


namespace Runtime.Structure.Rigging.Storage
{
    public class LiquidTank : Block, ILiquidTank
    {
        public LiquidType CurrentType => type;

        public Port<float> storage = new Port<float>(PortType.Fuel);

        public float amount;
        public float maximal;

        [SerializeField] private LiquidType type;



        public void FuelTick()
        {
            float delta = 1 - storage.Value;
            if (delta != 0)
            {
                float newAmount = Mathf.Clamp(amount + delta, 0, maximal);

                float deltaAmount = newAmount - amount;
                amount += deltaAmount;
                storage.Value -= deltaAmount;
            }
        }
    }
}