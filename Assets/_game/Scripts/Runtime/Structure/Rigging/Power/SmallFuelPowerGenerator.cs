using Core.Structure;
using Core.Structure.Rigging;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public class SmallFuelPowerGenerator : Block, IFuelPowerGenerator
    {
        public float MaximalOutput => maximalOutput;
        public float FuelConsumption => fuelPerSec;

        [Tooltip("Must be from zero to one")]
        public AnimationCurve powerPerFuel;
        [SerializeField] private float maximalOutput = 1;

        public float fuelUsage = 1;

        public Port<float> fuel = new Port<float>(PortType.Fuel);
        public PowerPort output = new PowerPort();

        private float fuelPerSec;
        private float currentOutput;
        [ShowInInspector, ReadOnly] private int powerUsage;

        public void FuelTick()
        {
            float amount = Mathf.Clamp(fuelUsage, 0f, fuel.Value);
            fuelPerSec = amount;
            fuel.Value -= amount * Time.deltaTime;
        }

        public void ConsumptionTick()
        {
            currentOutput = powerPerFuel.Evaluate(fuelPerSec) * maximalOutput;
            output.charge = currentOutput;
            output.maxOutput = Time.deltaTime;
        }
        public void PowerTick()
        {
            powerUsage = (int)((1 - (output.charge / currentOutput)) * 100f);
        }
    }
}