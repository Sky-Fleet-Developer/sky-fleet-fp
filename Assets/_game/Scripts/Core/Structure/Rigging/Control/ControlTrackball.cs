using System;
using Core.Character;
using Core.Data.GameSettings;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlTrackball : IControlElement
    {
        public bool EnableInteraction => enableInteraction;
        [SerializeField] private bool enableInteraction;
        public Transform Root => _device.transform;
        public (bool canInteract, string data) RequestInteractive(ICharacterController character)
        {
            return (true, string.Empty);
        }
        
        public enum TypeTrackballLimit
        {
            Rect = 0,
            Round = 1,
        }

        public string GetName()
        {
            return computerInput;
/*
            string keysDescr = string.Empty;
            if (!axisX.IsNone()) keysDescr += axisX.Axis.ToString();
            if (!axisY.IsNone()) keysDescr += "," + axisY.Axis.ToString();

            return keysDescr.Length == 0 ? computerInput : $"{computerInput} ({keysDescr})";*/
        }

        
        public Port GetPort() => port;

        [SerializeField, ShowInInspector]
        private Port<Vector2> port = new Port<Vector2>(PortType.Signal);

        public string computerInput;
        
        [ShowInInspector]
        public IDevice Device { get => _device; set => _device = (DeviceBase<Port<Vector2>>)value; }

        public void Init(IGraphHandler graph, IDriveInterface block)
        {
        }


        [SerializeField] protected InputControl.CorrectInputAxis axisX;
        [SerializeField] protected InputControl.CorrectInputAxis axisY;
        [SerializeField] protected TypeTrackballLimit typeLimit;
        [SerializeField, Range(0.1f, 4f)] protected float multiply = 1;

        [SerializeField, HideInInspector]
        private DeviceBase<Port<Vector2>> _device;

        private Vector2 currentPos = Vector3.zero;

        private Vector2 GetPos()
        {
            Vector2 retPos = currentPos;

            if (axisX.IsAbsolute())
            {
                float d = axisX.GetInputAbsolute();
                d *= multiply;
                retPos.x += d;
            }
            else
            {
                retPos.x = axisX.GetInputSum() * multiply;
            }
            if (axisY.IsAbsolute())
            {
                float d = axisY.GetInputAbsolute();
                d *= multiply;
                retPos.y += d;
            }
            else
            {
                retPos.y = axisY.GetInputSum() * multiply;
            }
            return retPos;
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
            _device.Port.Value = currentPos;
        }
        
        public void Enable()
        {
        }

        public void Disable()
        {
        }
    }
}