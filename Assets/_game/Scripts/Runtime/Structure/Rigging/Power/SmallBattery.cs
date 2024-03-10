using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public class SmallBattery : BlockWithNode, IPowerUser
    {
        public float maxInput = 1;
        public float maxOutput = 1;

        public PowerPort storage = new PowerPort();

        [SerializeField] private float storedPower;
        [SerializeField] private float maxStoredPower = 100;
        [ShowInInspector, ReadOnly] private int powerUsage;

        public void ConsumptionTick()
        {
            storage.maxOutput = Mathf.Max(Mathf.Min(maxOutput, storedPower), 0) * StructureUpdateModule.DeltaTime;
            storage.maxInput =  Mathf.Max(Mathf.Min(maxStoredPower - storedPower, maxInput * StructureUpdateModule.DeltaTime), 0);
            storage.charge = storedPower;
        }
        
        public void PowerTick()
        {
            storedPower = storage.charge;
            /*float delta = storage.GetDelta();
            storedPower += Mathf.Min(delta, possableInput);
            if (currentOutput == 0) powerUsage = 0;
            else powerUsage = (int)(-delta / currentOutput * 100f);*/
        }
    }
}