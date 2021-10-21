using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class LampIndicator : DeviceBase<bool>
    {
        [SerializeField] private MeshRenderer render;

        bool oldValue;

        public override void Init(IStructure structure, IBlock block)
        {
            base.Init(structure, block);
            oldValue = false;
            render.material.SetColor("EmissiveColor", Color.red);
        }

        public override void UpdateDevice()
        {
            if (oldValue != port.Value)
            {
                oldValue = port.Value;
                if (oldValue)
                {
                    render.material.SetColor("EmissiveColor", Color.green);
                }
                else
                {
                    render.material.SetColor("EmissiveColor", Color.red);
                }
            }
        }
    }
}