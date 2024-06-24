using System.Collections.Generic;
using System.Linq;
using Core.Structure;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Graph.Wires
{
    public enum PortType
    {
        Thrust,
        BigThrust,
        Button,
        Toggle,
        DoubleSignal,

    }
    

    public interface IPortUser
    {
        public Port GetPort();
        public string GetName();
    }
    
    [System.Serializable]
    public abstract class Port
    {
        protected Wire wire;

        public virtual void SetWire(Wire wire)
        {
            this.wire = wire;
        }
        public Wire GetWire() => wire;
        public abstract Wire CreateWire();

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

        public Wire()
        {
        }
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
       [System.NonSerialized, ShowInInspector, ReadOnly] private List<PowerPort>[] _ports = new List<PowerPort>[]
        {
            new List<PowerPort>(),
            new List<PowerPort>(),
            new List<PowerPort>()
        };

        public PowerWire()
        {
            StructureUpdateModule.OnConsumptionTickEnd += DistributionTick;
            StructureUpdateModule.OnBeginConsumptionTick += BeginConsumptionTick;
        }

        private void DistributionTick()
        {
            List<PowerPort> consumerPorts = _ports[(int) PortContentType.Consumer];
            List<PowerPort> generatorPorts = _ports[(int) PortContentType.Generator];
            List<PowerPort> storagePorts = _ports[(int) PortContentType.Storage];
            float generated = generatorPorts.Sum(x => Mathf.Min(x.charge, x.maxOutput));
            float wantedToConsume = consumerPorts.Sum(x => x.maxInput);
            float remains = generated - wantedToConsume;

            float toDistribution = generated;
            
            if (remains < 0)
            {
                float storedPotential = storagePorts.Sum(x => Mathf.Min(x.charge, x.maxOutput));
                float takenFromStorage = Mathf.Min(storedPotential, -remains);

                toDistribution += takenFromStorage;
                float power = wantedToConsume / toDistribution;
                float clampedPower = Mathf.Min(power, 1);
                
                for (int i = 0; i < consumerPorts.Count; i++)
                {
                    consumerPorts[i].charge = consumerPorts[i].maxInput * clampedPower;
                }

                for (int i = 0; i < generatorPorts.Count; i++)
                {
                    generatorPorts[i].charge = 0;
                }

                if (power < 1)
                {
                    float takenFromStoragePercent = takenFromStorage / storedPotential;
                    for (int i = 0; i < storagePorts.Count; i++)
                    {
                        storagePorts[i].charge -= Mathf.Min(storagePorts[i].charge, storagePorts[i].maxOutput) * takenFromStoragePercent;
                    }
                }
            }
            else
            {
                for (int i = 0; i < consumerPorts.Count; i++)
                {
                    consumerPorts[i].charge = consumerPorts[i].maxInput;
                }

                float maxStoragesConsumption = storagePorts.Sum(x => x.maxInput);
                float storePercent = remains / maxStoragesConsumption;
                for (int i = 0; i < storagePorts.Count; i++)
                {
                    storagePorts[i].charge += storagePorts[i].maxInput * storePercent;
                }
            }
        }
        
        private void BeginConsumptionTick()
        {
        }

        public override void Dispose()
        {
            StructureUpdateModule.OnConsumptionTickEnd -= DistributionTick;
            StructureUpdateModule.OnBeginConsumptionTick -= BeginConsumptionTick;
        }
        
        public void AddPort(Port port)
        {
            PowerPort powerPort = (PowerPort)port;
            _ports[(int)powerPort.GetContentType()].Add(powerPort);
        }

        public override bool CanConnect(PortPointer port)
        {
            return port.Port is PowerPort;
        }
    }

 
}
