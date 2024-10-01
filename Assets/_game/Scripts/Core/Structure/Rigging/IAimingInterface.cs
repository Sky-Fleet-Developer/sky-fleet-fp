using System;
using Core.Graph.Wires;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IAimingDevice
    {
        void SetState(AimingInterfaceState state);
    }
    
    public interface IAimingInterface
    {
        public Vector2 Input { get; set; }
        public event Action OnStateChanged;
        public AimingInterfaceState CurrentState { get; }
        public bool SetState(AimingInterfaceState state);
    }
    
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

    public enum AimingInterfaceState
    {
        Default = 0,
        FollowTarget = 1,
        Aiming = 2,
    }
}