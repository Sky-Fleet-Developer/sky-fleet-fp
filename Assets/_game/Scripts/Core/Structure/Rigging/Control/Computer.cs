using System.Collections.Generic;
using Core.Graph.Wires;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    public abstract class Computer : BlockWithNode, IComputer
    {
        public PowerPort power = new PowerPort();

        public bool IsWork { get; private set; }
        public float Consumption => maxConsumption;
        public PowerPort Power => power;

        public int updateFrequency = 1;

        [SerializeField] private float maxConsumption;

        private int countUpdatePassed = 0;

        public void ConsumptionTick()
        {
            Utilities.CalculateConsumerTickA(this);
        }

        public void PowerTick()
        {
            IsWork = Utilities.CalculateConsumerTickB(this);
        }

        public void UpdateBlock(int lod)
        {
            if (!IsWork) return;

            countUpdatePassed++;
            if(countUpdatePassed >= updateFrequency)
            {
                UpdateComputer();
                countUpdatePassed = 0;
            }
        }

        protected abstract void UpdateComputer();
    }
}