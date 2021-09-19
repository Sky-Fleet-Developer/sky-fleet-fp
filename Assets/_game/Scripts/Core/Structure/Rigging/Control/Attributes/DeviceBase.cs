using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Structure.Wires;
using UnityEngine;

namespace Structure.Rigging.Control.Attributes
{
    public abstract class DeviceBase<T> : MonoBehaviour, IDevice
    {
        public IStructure Structure => structure;
        public IBlock Block => block;
        public string Port { get; set; }

        protected IStructure structure;
        protected IBlock block;

        [ShowInInspector] protected Wire<T> wire;
        
        public virtual void Init(IStructure structure, IBlock block, string port)
        {
            this.structure = structure;
            this.block = block;
            this.Port = port;

            var p = Factory.GetAllPorts(structure).FirstOrDefault(x => x.Guid == port);
            if(p == null) return;
            if (p is Port<T> pT)
            {
                wire = pT.wire;
            }
        }

        public abstract void UpdateDevice();
    }
}
