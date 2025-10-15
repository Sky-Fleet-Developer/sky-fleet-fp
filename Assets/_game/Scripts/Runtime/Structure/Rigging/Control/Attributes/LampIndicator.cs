using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class LampIndicator : DeviceBase<Port<bool>>
    {
        public override Port<bool> Port => port;
        private Port<bool> port = new (PortType.Thrust);
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

        public override void Init(IGraph graph, IBlock block)
        {
            base.Init(graph, block);
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
        
        public override void MoveValueInteractive(float val)
        {
            throw new System.NotImplementedException();
        }

        public override void ExitControl()
        {
            throw new System.NotImplementedException();
        }

        public override bool EnableInteraction => false;
    }
}