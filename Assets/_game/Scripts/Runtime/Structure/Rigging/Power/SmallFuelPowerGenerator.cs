using Core.Structure;
using Core.Structure.Rigging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public class SmallFuelPowerGenerator : Block, IFuelPowerGenerator
    {
        public AnimationCurve powerPerFuel;
        public float powerMultiplier = 1;

        public float fuelUsage = 1;

        public Port<float> fuel = new Port<float>(PortType.Fuel);
        public Port<float> output = new Port<float>(PortType.Power);

        private float fuelPerSec;

        public void FuelTick()
        {
            float amount = Mathf.Clamp(fuelUsage, 0f, fuel.Value);
            fuelPerSec = amount;
            fuel.Value -= amount * Time.deltaTime;
        }

        public void PowerTick()
        {
            output.Value = powerPerFuel.Evaluate(fuelPerSec) * powerMultiplier;
        }
    }
}