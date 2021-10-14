using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Storage
{
    [System.Serializable]
    public abstract class StorageItem
    {
        public float amount;
    }
    [System.Serializable]
    public class WaterItem : StorageItem
    {
    }
    [System.Serializable]
    public class HydrogenItem : StorageItem
    {
    }
    
        
    [ShowInInspector]
    public class StorageWire : Wire
    {
        public StorageItem Item;
        public override bool CanConnect(Port port)
        {
            if (port is StoragePort portT)
            {
                return portT.ItemType == Item.GetType().Name;
            }
            return false;
        }
    }
    
    [System.Serializable]
    public class StoragePort : Port
    {
        [ShowInInspector, ValueDropdown("GetPossibleTypes")]
        public string ItemType;

        private IEnumerable<string> GetPossibleTypes() => Utilities.GetPossibleTypes();

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

        public StorageWire Wire;
        
        public StoragePort()
        {
            ItemType = "Null";
        }
        
        public StoragePort(System.Type itemType)
        {
            ItemType = itemType.Name;
        }
        
        public override void SetWire(Wire wire)
        {
            if (wire is StorageWire wireT) wire = wireT;
        }
        
        public override Wire CreateWire()
        {
            return new StorageWire();
        }

        public override bool CanConnect(Port port)
        {
            if (port is StoragePort portT)
            {
                return portT.ItemType == ItemType;
            }

            return false;
        }

        public override string ToString()
        {
            return ItemType;
        }
    }
}
