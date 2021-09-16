using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Structure.Rigging;
using Structure.Wires;
using UnityEngine;

namespace Structure.Movement
{
    public class ThursterTest : Block, IJetBlock
    {
        public AnimationCurve thurstPerFuel;
        public AnimationCurve fuelPerThrottle;
        
        public float MaximalThurst => maximalThurst;

        [SerializeField] private float maximalThurst;

        private IDynamicStructure root;

        public Port<float> throttle;
        public Port<float> fuel;

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