using Core.Graph.Wires;
using Core.Items;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateSystem;
using Utilities = Core.Structure.Rigging.Utilities;

namespace Runtime.Structure.Rigging.Storage
{
    public class Tank : BlockWithNode, ITank, IStatefulItemObject
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
            float clamp = Mathf.Min(Mathf.Max(amountToPull, -maxOutput * DeltaTime, -currentAmount), maxInput * DeltaTime, currentAmount);
            currentAmount += clamp;
            output.Value -= clamp;
        }
        
        [SerializeField, DrawWithUnity] private StorageMode mode;

        [SerializeField] private float maxInput;
        [SerializeField] private float maxOutput;
        [SerializeField] private float maximalAmount;
        [SerializeField] private float currentAmount;

        public StoragePort output = new StoragePort();

        string IStatefulItemObject.StateKey => "tank";

        string IStatefulItemObject.CaptureState() => ItemObjectState.FloatToState(currentAmount);

        void IStatefulItemObject.RestoreState(string state)
        {
            if (ItemObjectState.TryParseFloat(state, out float value))
            {
                currentAmount = Mathf.Clamp(value, 0f, maximalAmount);
            }
        }

        public void FuelTick()
        {
            if(output.Wire == null) return;
            Utilities.CalculateFuelTick(this);
        }
    }
}