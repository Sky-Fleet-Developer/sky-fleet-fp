using System.Collections.Generic;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure
{
    public enum PortType
    {
        Thurst,
        BigThurst,
        Fuel,
        Power,
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
        }
        
        [SerializeField, HideInInspector] private string guid;

        public PortType ValueType;

        public abstract void SetWire(Wire wire);
        public abstract Wire CreateWire();
        public abstract Wire GetWire();

        public void SetGUID(string guid)
        {
            this.guid = guid;
        }
    }

    [System.Serializable, InlineProperty(LabelWidth = 150)]
    public class Port<T> : Port
    {
        public T cache;

        public Wire<T> wire;

        public Port()
        {
        }

        public Port(PortType type)
        {
            ValueType = type;
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public T GetValue()
        {
            if (wire != null)
            {
                cache = wire.value;
            }
            return cache;
        }

        public void SetValue(T value)
        {
            if (wire != null)
            {
                wire.value = value;
            }
            cache = value;
        }

        public override void SetWire(Wire wire)
        {
            if (wire is Wire<T> wireT)
            {
                this.wire = wireT;
            }
        }

        public override Wire CreateWire()
        {
            return new Wire<T>();
        }

        public override Wire GetWire()
        {
            return wire;
        }
    }
    [System.Serializable, InlineProperty(LabelWidth = 150)]
    public class PowerPort : Port
    {
        public PowerWire wire;

        public float charge;
        public float maxInput = 1;
        public float maxOutput = 1;
        [ReadOnly] public float delta = 0;

        public PowerPort()
        {
            ValueType = PortType.Power;
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
                this.wire = wireT;
                wireT.ports.Add(this);
            }
        }

        public override Wire CreateWire()
        {
            return new PowerWire();
        }

        public override Wire GetWire()
        {
            return wire;
        }
    }

    [ShowInInspector]
    public class Wire : System.IDisposable
    {
        public virtual void Dispose() { }
    }

    [ShowInInspector]
    public class Wire<T> : Wire
    {
        public T value;
    }
    [ShowInInspector]
    public class PowerWire : Wire
    {
        [System.NonSerialized, ShowInInspector, ReadOnly] public List<PowerPort> ports = new List<PowerPort>();


        public PowerWire()
        {
            StructureUpdateModule.onConsumptionTickEnd += ConsumptionTickEnd;
            StructureUpdateModule.onBeginConsumptionTick += BeginConsumptionTick;
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
            StructureUpdateModule.onConsumptionTickEnd -= ConsumptionTickEnd;
            StructureUpdateModule.onBeginConsumptionTick -= BeginConsumptionTick;
        }

    }

}
