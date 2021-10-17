using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class EasyDevice<T> : DeviceBase<T>
    {
        [SerializeField] protected Transform visualTransfrom;
        
        protected bool IsMinLod { get; private set; }

        int oldLod = -1;

        protected const int maxLod = 0;

        public override void UpdateDevice(int lod)
        {
            if (oldLod == lod) return; 
            
            oldLod = lod;
            bool enable = lod <= maxLod;
            visualTransfrom.gameObject.SetActive(enable);
            IsMinLod = enable;
        }
    }
}