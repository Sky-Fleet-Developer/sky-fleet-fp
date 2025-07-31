using Core.Graph.Wires;

namespace Core.Structure.Rigging
{
    public class AimingInterfacePort : Port
    {
        private IAimingInterface myAimingInterface;
        private IAimingDevice myAimingDevice;
        public AimingInterfaceBinding Binding { get; private set; }
        private bool isInitialized;
        public bool Initialized => isInitialized;
        public AimingInterfacePort(IAimingInterface aimingInterface)
        {
            myAimingInterface = aimingInterface;
        }
        public AimingInterfacePort(IAimingDevice aimingDevice)
        {
            myAimingDevice = aimingDevice;
        }
        
        public override Wire CreateWire()
        {
            return new AimingInterfaceBinding();
        }

        public override bool CanConnect(Port port)
        {
            if (port is not AimingInterfacePort portT)
            {
                return false;
            }
            if (myAimingInterface != null && portT.myAimingInterface != null || myAimingDevice != null && portT.myAimingDevice != null)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "Aiming interface";
        }

        public override void SetWire(Wire wire)
        {
            base.SetWire(wire);
            Binding = (AimingInterfaceBinding)wire; 
            if (myAimingDevice != null)
            {
                Binding.AimingDevice = myAimingDevice;
            }
            if (myAimingInterface != null)
            {
                Binding.AimingInterface = myAimingInterface;
            }

            if (Binding is {AimingDevice: { }, AimingInterface: { }})
            {
                isInitialized = true;
                foreach (var portPointer in wire.ports)
                {
                    ((AimingInterfacePort) portPointer.Port).isInitialized = true;
                }
            }
        }
    }
}