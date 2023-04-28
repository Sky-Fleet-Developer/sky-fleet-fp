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

        protected List<PortPointer> inputsPort = new List<PortPointer>();
        protected List<PortPointer> outputPort = new List<PortPointer>();

        private int countUpdatePassed = 0;

        public void ConsumptionTick()
        {
            Utilities.CalculateConsumerTickA(this);
        }

        public void PowerTick()
        {
            IsWork = Utilities.CalculateConsumerTickB(this);
        }

        public IEnumerable<PortPointer> GetPorts()
        {
            foreach (PortPointer iPort in inputsPort)
            {
                yield return iPort;
            }
            foreach (PortPointer oPort in outputPort)
            {
                yield return oPort;
            }
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