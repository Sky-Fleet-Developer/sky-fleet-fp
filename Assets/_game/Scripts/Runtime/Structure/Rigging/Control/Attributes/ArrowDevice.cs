using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ArrowDevice : DeviceBase, IDeviceWithPort
    {
        [SerializeField] private string deviceName;
        [SerializeField] private Transform arrow;
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private Vector2 map;
        [SerializeField] private bool clamp;
        public Port<float> value = new(PortType.Signal);
        private Vector3 _eulerStart;

        public Port GetPort() => value;

        public string GetName() => deviceName;

        private void Awake()
        {
            _eulerStart = arrow.localRotation.eulerAngles;
        }

        public override void UpdateDevice()
        {
            float v = map.x + value.GetValue() * (map.y - map.x);
            if (clamp)
            {
                v = Mathf.Clamp(v, map.x, map.y);
            }
            arrow.localRotation = Quaternion.Euler(_eulerStart) * Quaternion.AngleAxis(v, axis);
        }
    }
}