using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class SimpleThruster : BlockWithNode, IJet
    {
        public AnimationCurve thrustPerFuel;
        public AnimationCurve fuelPerThrottle;
        public float MaximalThrust => maximalThrust;
        public float CurrentThrust => currentThrust; 

        [SerializeField] private float maximalThrust;
        [SerializeField] private float fuelConsumptionMul = 1;

        protected IDynamicStructure root;

        public Port<float> throttle = new Port<float>(PortType.Thrust);
        public StoragePort fuel = new StoragePort(typeof(Hydrogen));

        private float fuelPerSec;
        [ShowInInspector, ReadOnly] private float currentThrust;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            if (structure is IDynamicStructure dynamicStructure) root = dynamicStructure;
        }

        void IFuelUser.FuelTick()
        {
            float amount = Mathf.Clamp(fuelPerThrottle.Evaluate(throttle.Value) * fuelConsumptionMul, 0f, fuel.Value);
            fuelPerSec = amount;
            fuel.Value -= amount.DeltaTime();
        }
        
        void IForceUser.ApplyForce()
        {
            currentThrust = maximalThrust * thrustPerFuel.Evaluate(fuelPerSec / fuelConsumptionMul);
            ApplyThrust(currentThrust);
        }

        protected virtual void ApplyThrust(float thrust)
        {
            root.AddForce(transform.forward * thrust, transform.position);
        }
    }
}