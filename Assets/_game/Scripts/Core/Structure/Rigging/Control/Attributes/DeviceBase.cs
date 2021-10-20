using Core.Structure.Wires;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public abstract class DeviceBase : MonoBehaviour, IDevice
    {
        public IStructure Structure => structure;
        public IBlock Block => block;

        protected IStructure structure;
        protected IBlock block;

        public virtual void Init(IStructure structure, IBlock block)
        {
            this.structure = structure;
            this.block = block;
        }

        public virtual void UpdateDevice()
        {
        }
    }

    public abstract class DeviceBase<T> : DeviceBase
    {
        public Port<T> port = new Port<T>();
    }
}
