using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Utilities;
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

        public StorageWire(StorageItem item)
        {
            Item = item;
        }
        public override bool CanConnect(Port port)
        {
            if (port is StoragePort portT)
            {
                return portT.serializedType == Item.GetType().Name;
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
                serializedType = value == null ? "Null" : value.FullName;
            }
        }
        private System.Type itemType = null;
        [ReadOnly] public string serializedType;

        private IEnumerable<System.Type> GetPossibleTypes() => Utilities.GetPossibleTypes();

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
            if (wire is StorageWire wireT) Wire = wireT;
        }
        
        public override Wire CreateWire()
        {
            Type type = TypeExtensions.GetTypeByName(serializedType);
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
            return serializedType;
        }
    }
}
