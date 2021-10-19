using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Storage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class ThrusterTest : Block, IJet
    {
        public AnimationCurve thrustPerFuel;
        public AnimationCurve fuelPerThrottle;
        public float MaximalThrust => maximalThrust;
        public float CurrentThrust => currentThrust; 

        [SerializeField] private float maximalThrust;

        private IDynamicStructure root;

        public Port<float> throttle = new Port<float>(PortType.Thrust);
        public StoragePort fuel = new StoragePort(typeof(Hydrogen));

        private float fuelPerSec;
        [ShowInInspector, ReadOnly] private float currentThrust;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            if (structure is IDynamicStructure dynamicStructure) root = dynamicStructure;
        }

        public void FuelTick()
        {
            float amount = Mathf.Clamp(fuelPerThrottle.Evaluate(throttle.Value), 0f, fuel.Value);
            fuelPerSec = amount;
            fuel.Value -= amount * Time.deltaTime;
        }
        
        public void ApplyForce()
        {
            currentThrust = maximalThrust * thrustPerFuel.Evaluate(fuelPerSec);
            root.AddForce(transform.forward * (currentThrust * Time.deltaTime), transform.position);
        }
    }
}