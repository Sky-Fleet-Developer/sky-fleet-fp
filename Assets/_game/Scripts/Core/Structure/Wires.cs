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

    [System.Serializable]
    public abstract class Port
    {
        [ShowInInspector] public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
                return guid;
            }
        }

        [SerializeField, HideInInspector] private string guid;

        public PortType TypeValue;

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
        public T hash;

        public Wire<T> wire;

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public T GetValue()
        {
            if (wire != null)
            {
                hash = wire.value;
            }
            return hash;
        }

        public void SetValue(T value)
        {
            if (wire != null)
            {
                wire.value = value;
            }
            hash = value;
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

    [System.Serializable]
    public class Wire
    {
        
    }
    
    [System.Serializable]
    public class Wire<T> : Wire
    {
        public T value;
    }
}
