using System;
using Core.Structure.Rigging.Control.Attributes;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlTrackball : IControlElement
    {
        public enum TypeTrackballLimit
        {
            Rect = 0,
            Round = 1,
        }

        public string GetPortDescription()
        {
            string keysDescr = string.Empty;
            if (!axisX.IsNone()) keysDescr += axisX.GetNameAxe();
            if (!axisY.IsNone()) keysDescr += "," + axisY.GetNameAxe();

            return keysDescr.Length == 0 ? computerInput : $"{computerInput} ({keysDescr})";
        }

        
        public Port GetPort() => port;

        [SerializeField, ShowInInspector]
        private Port<Vector2> port = new Port<Vector2>(PortType.DoubleSignal);

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Vector2>)value; }

        public void Init(IStructure structure, IControl block)
        {
            structure.ConnectPorts(port, _device.port);
        }


        [SerializeField] protected AxeInput axisX;
        [SerializeField] protected AxeInput axisY;
        [SerializeField] protected TypeTrackballLimit typeLimit;
        [SerializeField, Range(0.1f, 4f)] protected float multiply = 1;

        [SerializeField, HideInInspector]
        private DeviceBase<Vector2> _device;

        private Vector2 currentPos = Vector3.zero;

        private Vector2 GetPos()
        {

            Vector2 delta = new Vector2(axisX.GetValue(), -axisY.GetValue());
            delta.x = delta.x * multiply;
            delta.y = delta.y * multiply;
            return currentPos + delta;
        }

        public void Tick()
        {
            Vector2 pos = GetPos();
            if(typeLimit == TypeTrackballLimit.Rect)
            {
                pos.x = Mathf.Clamp(pos.x, -1, 1);
                pos.y = Mathf.Clamp(pos.y, -1, 1);
            }
            else
            {
                pos = Vector2.ClampMagnitude(pos, 1);
            }
            currentPos = pos;
            port.Value = currentPos;

        }
    }
}