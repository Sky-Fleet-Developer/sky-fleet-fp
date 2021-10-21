using System.Collections.Generic;
using System.Linq;
using Core.Utilities;
using Sirenix.OdinInspector;

namespace Core.Structure.Wires
{
    [System.Serializable]
    public abstract class StorageItem
    {
        public float amount;
    }
    [System.Serializable]
    public class Water : StorageItem
    {
    }
    [System.Serializable]
    public class Hydrogen : StorageItem
    {
    }
    
        
    [ShowInInspector]
    public class StorageWire : Wire
    {
        public StorageItem Item;

        public StorageWire(StorageItem item)
        {
            Item = item;
        }
        public override bool CanConnect(PortPointer port)
        {
            if (port.Port is StoragePort portT)
            {
                return portT.serializedType == Item.GetType().FullName;
            }
            return false;
        }
    }
    
    [System.Serializable]
    public class StoragePort : Port
    {
        [ShowInInspector, ValueDropdown("GetPossibleTypes")]
        public System.Type ItemType
        {
            get
            {
                return serializedType == "Null" ? null : GetPossibleTypes().FirstOrDefault(x => x != null && x.FullName == serializedType);
            }
            set
            {
                itemType = value;
                if (value == null || value == typeof(StorageItem))
                {
                    serializedType = "Null";
                    serializedTypeShort = "Store";
                }
                else
                {
                    serializedType = value.FullName;
                    serializedTypeShort = value.Name;
                }
            }
        }
        private System.Type itemType = null;
        [ReadOnly] public string serializedType;
        [ReadOnly] public string serializedTypeShort;

        private IEnumerable<System.Type> GetPossibleTypes() => Utilities.GetStorageItemTypes();

        [ShowInInspector]
        public float Value
        {
            get => Wire?.Item.amount ?? 0f;
            set
            {
                if(Wire == null) return;
                Wire.Item.amount = value;
            }
        }

        [ShowInInspector]
        public StorageWire Wire;
        
        public StoragePort()
        {
            serializedType = "Null";
        }
        
        public StoragePort(System.Type itemType)
        {
            serializedType = itemType.Name;
        }
        
        public override void SetWire(Wire wire)
        {
            if (wire is StorageWire wireT)
            {
                Wire = wireT;
                this.wire = Wire;
            }
        }
        
        public override Wire CreateWire()
        {
            System.Type type = TypeExtensions.GetTypeByName(serializedType);
            StorageItem itemInstance = (StorageItem) System.Activator.CreateInstance(type);
            return new StorageWire(itemInstance);
        }

        public override bool CanConnect(Port port)
        {
            if (port is StoragePort portT)
            {
                return portT.serializedType == serializedType;
            }

            return false;
        }

        public override string ToString()
        {
            return serializedTypeShort;
        }
    }
}
