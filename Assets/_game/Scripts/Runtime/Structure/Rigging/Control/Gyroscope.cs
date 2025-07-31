using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class Gyroscope : BlockWithNode, IGyroscope
    {
        public Port<float> localSpeedX = new Port<float>(PortType.Thrust);
        public Port<float> localSpeedY = new Port<float>(PortType.Thrust);
        public Port<float> localSpeedZ = new Port<float>(PortType.Thrust);
        public Port<float> localAngularSpeedX = new Port<float>(PortType.Thrust);
        public Port<float> localAngularSpeedY = new Port<float>(PortType.Thrust);
        public Port<float> localAngularSpeedZ = new Port<float>(PortType.Thrust);
        public Port<float> pitch = new Port<float>(PortType.Thrust);
        public Port<float> roll = new Port<float>(PortType.Thrust);

        public bool IsWork { get; private set; }
        public float Consumption => maxConsumption;
        public PowerPort Power => power;
        [SerializeField] private float maxConsumption;
        public PowerPort power = new PowerPort();

        private IDynamicStructure root;
        
        //[SerializeField] private List<PortPointer> cache;
        
        
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            if (structure is IDynamicStructure dynamicStructure) root = dynamicStructure;
            /*cache = new List<PortPointer>()
            {
                new PortPointer(this, localSpeedX),
                new PortPointer(this, localSpeedY),
                new PortPointer(this, localSpeedZ),
                new PortPointer(this, localAngularSpeedX),
                new PortPointer(this, localAngularSpeedY),
                new PortPointer(this, localAngularSpeedZ),
                new PortPointer(this, pitch),
                new PortPointer(this, roll),
                new PortPointer(this, power),
            };*/
        }

        public void UpdateBlock(int lod)
        {
            if (!IsWork) return;
            
            Vector3 localSpeed = root.transform.InverseTransformVector(root.Physics.velocity);
            Vector3 localAngleSpeed = root.transform.InverseTransformVector(root.Physics.angularVelocity);
            pitch.Value = Mathf.Asin(root.transform.forward.y) * Mathf.Rad2Deg;
            roll.Value = -Mathf.Atan2(root.transform.right.y, root.transform.up.y) * Mathf.Rad2Deg;
            localSpeedX.Value = localSpeed.x;
            localSpeedY.Value = localSpeed.y;
            localSpeedZ.Value = localSpeed.z;
            localAngularSpeedX.Value = localAngleSpeed.x * Mathf.Rad2Deg;
            localAngularSpeedY.Value = localAngleSpeed.y * Mathf.Rad2Deg;
            localAngularSpeedZ.Value = localAngleSpeed.z * Mathf.Rad2Deg;
        }

        public void ConsumptionTick()
        {
            this.CalculateConsumerTickA();
        }

        public void PowerTick()
        {
            IsWork = this.CalculateConsumerTickB();
        }
    }
}