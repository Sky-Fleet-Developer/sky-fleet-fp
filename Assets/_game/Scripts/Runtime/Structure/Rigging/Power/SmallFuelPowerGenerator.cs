using Core.Structure;
using Core.Structure.Rigging;
using System.Collections;
using System.Collections.Generic;
using Core.Graph.Wires;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public class SmallFuelPowerGenerator : BlockWithNode, IFuelPowerGenerator
    {
        public float MaximalOutput => maximalOutput;
        public float FuelConsumption => fuelPerSec;
        public float MaxFuelConsumption => maxFuelConsumption;
        public float CurrentFuelConsumption => autoThrottle * maxFuelConsumption;
        public float CurrentPowerUsage => powerUsage;

        [Tooltip("Must be from zero to one")]
        public AnimationCurve powerPerFuel;
        [SerializeField] private float maximalOutput = 1;
        [SerializeField] private float charge = 500;

        public float maxFuelConsumption = 1;
        public float minFuelUsage = 1;

        public StoragePort fuel = new StoragePort(typeof(Hydrogen));
        public PowerPort output = new PowerPort();

        private float fuelPerSec;
        private float currentOutput;
        [ShowInInspector, ReadOnly] private float powerUsage;
        [ShowInInspector, ReadOnly] private float autoThrottle;

        public void FuelTick()
        {
            if (fuel.Wire == null)
            {
                autoThrottle = 0;
                fuelPerSec = 0;
                return;
            }
            autoThrottle = Mathf.MoveTowards(autoThrottle, powerUsage, Time.deltaTime);
            fuelPerSec = Mathf.Clamp(maxFuelConsumption * autoThrottle, minFuelUsage, fuel.Value);
            fuel.Value -= fuelPerSec * Time.deltaTime;
        }

        public void ConsumptionTick()
        {
            currentOutput = powerPerFuel.Evaluate(fuelPerSec) * maximalOutput *  Time.deltaTime;
            output.charge = charge;
            output.maxOutput = currentOutput;
        }
        public void PowerTick()
        {
            if (currentOutput == 0) powerUsage = 0;
            else powerUsage = (charge - output.charge) / currentOutput;
        }
    }
}