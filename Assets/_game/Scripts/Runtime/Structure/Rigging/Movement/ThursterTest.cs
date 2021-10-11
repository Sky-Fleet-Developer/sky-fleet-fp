using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class ThursterTest : Block, IJet
    {
        public AnimationCurve thurstPerFuel;
        public AnimationCurve fuelPerThrottle;
        
        public float MaximalThurst => maximalThurst;

        [SerializeField] private float maximalThurst;

        private IDynamicStructure root;

        public Port<float> throttle = new Port<float>(PortType.Thurst);
        public Port<float> fuel = new Port<float>(PortType.Fuel);

        private float fuelPerSec;
        [ShowInInspector, ReadOnly] private float currentThurst;

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
            currentThurst = maximalThurst * thurstPerFuel.Evaluate(fuelPerSec);
            root.AddForce(transform.forward * (currentThurst * Time.deltaTime), transform.position);
        }
    }
}