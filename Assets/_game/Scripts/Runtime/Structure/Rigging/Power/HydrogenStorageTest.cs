using Core.Graph.Wires;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;
using Utilities = Core.Structure.Rigging.Utilities;

namespace Runtime.Structure.Rigging.Power
{
    public class HydrogenStorageTest : BlockWithNode, ITank
    {
        public float CurrentAmount => currentAmount;
        public float MaximalAmount => maximalAmount;
        public float MaxInput => maxInput;
        public float MaxOutput => maxOutput;
        public float AmountInPort => output.Value;
        [PlayerProperty] public StorageMode Mode { get => mode; set => mode = value; }
        public void PushToPort(float amount)
        {
            float amountToPull = -amount;
            float clamp = Mathf.Min(Mathf.Max(amountToPull, -maxOutput * DeltaTime, -currentAmount), maxInput * DeltaTime, currentAmount, output.Value);
            currentAmount += clamp;
            output.Value -= clamp;
        }
        

        [SerializeField] private StorageMode mode;

        [SerializeField] private float maxInput;
        [SerializeField] private float maxOutput;
        [SerializeField] private float maximalAmount;
        [SerializeField] private float currentAmount;

        public StoragePort output = new StoragePort(typeof(Hydrogen));

        public void FuelTick()
        {
            if(output.Wire == null) return;
            Utilities.CalculateFuelTick(this);
        }
    }
}
