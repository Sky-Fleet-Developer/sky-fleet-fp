using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Structure.Rigging.Control.Attributes;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public abstract class Computer : Block, IComputer
    {

        public PowerPort power = new PowerPort();

        public bool IsWork { get; private set; }

        public int CountUpdate = 1;

        [SerializeField] private float maxConsumption;

        protected List<PortPointer> inputsPort = new List<PortPointer>();
        protected List<PortPointer> outputPort = new List<PortPointer>();

        private int countUpdatePassed = 0;

        private const float deltaConsumption = 0.02f;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
        }

        public void ConsumptionTick()
        {
            power.charge = 0;
            power.maxInput = maxConsumption;
            power.maxOutput = 0;
        }

        public void PowerTick()
        {
            float consumption = power.charge;
            IsWork = deltaConsumption * maxConsumption > Mathf.Abs(consumption - maxConsumption);
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
            if(countUpdatePassed >= CountUpdate)
            {
                UpdateComputer();
                countUpdatePassed = 0;
            }
        }

        protected abstract void UpdateComputer();
    }
}