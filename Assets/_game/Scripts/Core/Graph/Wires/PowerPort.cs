using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Graph.Wires
{
    public enum PortContentType
    {
        Consumer = 0,
        Storage = 1,
        Generator = 2
    }
    
    [System.Serializable, InlineProperty(LabelWidth = 150)]
    public class PowerPort : Port
    {
        [ShowInInspector] public PowerWire Wire;

        public float charge;
        public float maxInput = 1;
        public float maxOutput = 1;
        [SerializeField] private PortContentType contentType;
        [ReadOnly] public float delta = 0;

        public PortContentType GetContentType()
        {
            return contentType;
        }
        
        public float GetPushValue()
        {
            float clamp = Mathf.Clamp(delta, -maxOutput, maxInput);
            return clamp - delta;
        }

        public float GetSpaceToUpLimit()
        {
            return Mathf.Max(maxInput - delta, 0f);
        }
        public float GetSpaceToDownLimit()
        {
            return Mathf.Min(-maxOutput - delta, 0f);
        }
        
        public override void SetWire(Wire wire)
        {
            if (wire is PowerWire wireT)
            {
                Wire = wireT;
                wireT.AddPort(this);
                this.wire = Wire;
            }
        }

        public override Wire CreateWire()
        {
            return new PowerWire();
        }
        
        public override bool CanConnect(Port port)
        {
            return port is PowerPort portT;
        }
        
        public override string ToString()
        {
            return "Power";
        }
    }
}