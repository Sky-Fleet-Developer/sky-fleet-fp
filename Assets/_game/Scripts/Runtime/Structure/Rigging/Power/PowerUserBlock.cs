using Core.Graph.Wires;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Power
{
    public abstract class PowerUserBlock : BlockWithNode, IConsumer
    {
        [SerializeField] private PowerPort power = new PowerPort();

        public bool IsWork { get; private set; }
        public abstract float Consumption { get; }
        public PowerPort Power => power;
        
        public virtual void ConsumptionTick()
        {
            this.CalculateConsumerTickA();
        }

        public virtual void PowerTick()
        {
            IsWork = this.CalculateConsumerTickB();
        }
    }
}