using Core.Graph.Wires;

namespace Core.Structure.Rigging
{
    public class AimingInterfaceBinding : Wire
    {
        public IAimingInterface AimingInterface;
        public IAimingDevice AimingDevice;
        
        public override bool CanConnect(PortPointer port)
        {
            if (port.Port is AimingInterfacePort portT)
            {
                if (portT.Binding == null)
                {
                    return true;
                }
                if (AimingInterface != null && portT.Binding.AimingInterface != null || AimingDevice != null && portT.Binding.AimingDevice != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}