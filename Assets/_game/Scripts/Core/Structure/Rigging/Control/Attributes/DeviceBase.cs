using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public abstract class DeviceBase : MonoBehaviour, IDevice
    {
        public IStructure Structure => structure;
        public IBlock Block => block;
        public string Port { get; set; }

        protected IStructure structure;
        protected IBlock block;
        
        public virtual void Init(IStructure structure, IBlock block, string port)
        {
            this.structure = structure;
            this.block = block;
            this.Port = port;

            var p = Factory.GetAllPorts(structure).FirstOrDefault(x => x.Guid == port);
            if(p == null) return;
            SetWire(p);
        }

        protected abstract void SetWire(Port p);
        public abstract void UpdateDevice();
    }

    public abstract class DeviceBase<T> : DeviceBase
    {
        [ShowInInspector] protected Wire<T> wire;

        protected override void SetWire(Port p)
        {
            if (p is Port<T> pT)
            {
                wire = pT.wire;
            }
        }
    }
}
