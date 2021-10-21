using System.Collections.Generic;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Wires
{
    public enum PortType
    {
        Thrust,
        BigThrust,
        Button,
        Toggle,
        DoubleSignal
    }

    public interface IMultiplePorts : IBlock
    {
        IEnumerable<PortPointer> GetPorts();
    }

    public interface IPortUser
    {
        public Port GetPort();
        public string GetPortDescription();
    }
    
    public struct PortPointer : System.IEquatable<PortPointer>, System.IEquatable<string>
    {
        public readonly IBlock Block;
        public readonly  Port Port;

        public readonly string Id;
        
        public PortPointer(IBlock block, Port port)
        {
            Block = block;
            Port = port;
            Id = Block.transform.name + Port.Guid;
        }

        public override string ToString()
        {
            return Id;
        }

        public bool Equals(PortPointer other)
        {
            return Id == other.Id;
        }

        public bool Equals(string other)
        {
            return Id.Equals(other);
        }

        public override bool Equals(object obj)
        {
            return obj is PortPointer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        public bool CanConnect(Wire wire) => wire.CanConnect(this);
    }

    [System.Serializable]
    public abstract class Port
    {
        [ShowInInspector]
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
                return guid;
            }
            private set => guid = value;
        }
        
        [SerializeField, HideInInspector] private string guid;

        protected Wire wire;

        public virtual void SetWire(Wire wire)
        {
            this.wire = wire;
        }
        public Wire GetWire() => wire;
        public abstract Wire CreateWire();

        public void SetGuid(string guid)
        {
            this.guid = guid;
        }

        public abstract bool CanConnect(Port port);
        public abstract string ToString();
    }

    [System.Serializable, InlineProperty(LabelWidth = 150)]
    public class Port<T> : Port
    {
        public T cache;

        [ShowInInspector] public Wire<T> Wire;

        public PortType ValueType => valueType;
        [SerializeField] private PortType valueType;
        
        public Port()
        {
        }

        public Port(PortType type)
        {
            valueType = type;
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public T GetValue()
        {
            if (Wire != null)
            {
                cache = Wire.value;
            }
            return cache;
        }

        public void SetValue(T value)
        {
            if (Wire != null)
            {
                Wire.value = value;
            }
            cache = value;
        }

        public override void SetWire(Wire wire)
        {
            if (wire is Wire<T> wireT)
            {
                Wire = wireT;
                this.wire = Wire;
            }
        }

        public override Wire CreateWire()
        {
            return new Wire<T>(valueType);
        }

        public override bool CanConnect(Port port)
        {
            if (port is Port<T> portT) return portT.valueType == valueType;
            return false;
        }

        public override string ToString()
        {
            return valueType.ToString() + " Wire type: " + typeof(T).Name;
        }
    }
    [System.Serializable, InlineProperty(LabelWidth = 150)]
    public class PowerPort : Port
    {
        [ShowInInspector] public PowerWire Wire;

        public float charge;
        public float maxInput = 1;
        public float maxOutput = 1;
        [ReadOnly] public float delta = 0;

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
                wireT.ports.Add(this);
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

    [ShowInInspector]
    public abstract class Wire : System.IDisposable
    {
        public List<PortPointer> ports = new List<PortPointer>();
        public virtual void Dispose() { }

        public abstract bool CanConnect(PortPointer port);
    }

    [ShowInInspector]
    public class Wire<T> : Wire
    {
        public T value;
        
        public PortType ValueType => valueType;
        protected PortType valueType;

        public Wire(PortType type)
        {
            valueType = type;
        }

        public override bool CanConnect(PortPointer port)
        {
            if (port.Port is Port<T> portT) return portT.ValueType == valueType;
            return false;
        }
    }
    [ShowInInspector]
    public class PowerWire : Wire
    {
        [System.NonSerialized, ShowInInspector, ReadOnly] public List<PowerPort> ports = new List<PowerPort>();


        public PowerWire()
        {
            StructureUpdateModule.OnConsumptionTickEnd += ConsumptionTickEnd;
            StructureUpdateModule.OnBeginConsumptionTick += BeginConsumptionTick;
        }
        private void ConsumptionTickEnd()
        {
            for (int i = 0; i < ports.Count; i++)
            {
                float opposit = 0;
                for (int i1 = 0; i1 < ports.Count; i1++)
                {
                    if (i != i1)
                    {
                        opposit += ports[i1].charge;
                    }
                }
                ports[i].delta = Mathf.Clamp(opposit - ports[i].charge, -ports[i].maxOutput, ports[i].maxInput);
            }

            float othersCountInv = 1f / (ports.Count - 1);
            float[] cache = new float[ports.Count];

            for (int i = 0; i < ports.Count; i++)
            {
                float deltaFromOthers = 0;
                for (int i1 = 0; i1 < ports.Count; i1++)
                {
                    if (i != i1)
                    {
                        deltaFromOthers += ports[i1].delta * othersCountInv;
                    }
                }
                cache[i] = ports[i].delta - deltaFromOthers;
            }

            for (int i = 0; i < ports.Count; i++)
            {
                ports[i].delta = cache[i];
            }
            
            for (int i1 = 0; i1 < cache.Length; i1++)
            {
                cache[i1] = 0;
            }
            
            for (int i = 0; i < ports.Count; i++)
            {
                float wantsToPush = ports[i].GetPushValue();
                bool pushPositive = wantsToPush > 0;
                float pushSpace = 0;
                if (wantsToPush != 0)
                {
                    for (int i1 = 0; i1 < ports.Count; i1++)
                    {
                        if (i != i1)
                        {
                            cache[i1] = pushPositive ? ports[i1].GetSpaceToDownLimit() : ports[i1].GetSpaceToUpLimit();
                            pushSpace += cache[i1];
                        }
                    }

                    if(pushSpace == 0) continue;
                    float mul = wantsToPush / pushSpace;
                    for (int i1 = 0; i1 < cache.Length; i1++)
                    {
                        ports[i1].delta -= cache[i1] * mul;
                    }

                    ports[i].delta += wantsToPush;
                    
                    for (int i1 = 0; i1 < cache.Length; i1++)
                    {
                        cache[i1] = 0;
                    }
                }
            }

            for (int i = 0; i < ports.Count; i++)
            {
                ports[i].charge += ports[i].delta;
            }
        }
        
        private void BeginConsumptionTick()
        {
        }

        public override void Dispose()
        {
            StructureUpdateModule.OnConsumptionTickEnd -= ConsumptionTickEnd;
            StructureUpdateModule.OnBeginConsumptionTick -= BeginConsumptionTick;
        }

        public override bool CanConnect(PortPointer port)
        {
            return port.Port is PowerPort;
        }
    }

}
