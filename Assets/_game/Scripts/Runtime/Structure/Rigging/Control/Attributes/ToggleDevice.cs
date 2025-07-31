using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ToggleDevice : DeviceBase<Port<bool>>
    {
        public override Port<bool> Port => port;
        private Port<bool> port = new (PortType.Thrust);
        [SerializeField] private Transform ball;
        [SerializeField, Range(0, 2.0f)] private float minPos;
        [SerializeField] private float sensitivity;
        private bool _valueChangedOnInteractive;
        public override void MoveValueInteractive(float val)
        {
            if (Mathf.Abs(val) > sensitivity)
            {
                _valueChangedOnInteractive = true;
                Port.Value = val > 0;
            }
        }

        public override void ExitControl()
        {
            if (!_valueChangedOnInteractive)
            {
                Port.Value = !Port.Value;
            }
            _valueChangedOnInteractive = false;
        }

        public override void UpdateDevice()
        {
            if (port.Value)
            {
                ball.localPosition = new Vector3(0, -minPos, 0);
            }
            else
            {
                ball.localPosition = Vector3.zero;
            }
        }
    }
}
