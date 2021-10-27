using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class LampIndicator : DeviceBase<bool>
    {
        [SerializeField] private Color active;
        [SerializeField] private Color inactive;

        [SerializeField] private MeshRenderer render;

        bool oldValue;

        private int emissive = Shader.PropertyToID("_EmissiveColor");

        private void Awake()
        {
            render.material.color = inactive;
            render.material.SetColor(emissive, inactive);
        }

        public override void Init(IStructure structure, IBlock block)
        {
            base.Init(structure, block);
            oldValue = false;
            render.material.color = inactive;
            render.material.SetColor(emissive, inactive);
        }

        public override void UpdateDevice()
        {
            if (oldValue != port.Value)
            {
                oldValue = port.Value;
                if (oldValue)
                {
                    render.material.color = active;
                    render.material.SetColor(emissive, active);
                }
                else
                {
                    render.material.color = inactive;
                    render.material.SetColor(emissive, inactive);
                }
            }
        }
    }
}