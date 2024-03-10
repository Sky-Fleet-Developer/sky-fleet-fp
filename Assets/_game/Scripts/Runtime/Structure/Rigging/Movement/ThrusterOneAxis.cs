using Core.Graph.Wires;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class ThrusterOneAxis : SimpleThruster, IUpdatableBlock
    {
        public Port<float> vector = new Port<float>(PortType.Thrust);
        [SerializeField] private Transform nozzle;
        [SerializeField] private float maxInclinationAngle = 30;
        
        protected override void ApplyThrust(float thrust)
        {
            root.AddForce(nozzle.forward * thrust, nozzle.position);
        }

        public void UpdateBlock(int lod)
        {
            nozzle.localRotation = Quaternion.Euler(Vector3.up * (Mathf.Clamp(vector.GetValue(), -1, 1) * maxInclinationAngle));
        }
    }
}