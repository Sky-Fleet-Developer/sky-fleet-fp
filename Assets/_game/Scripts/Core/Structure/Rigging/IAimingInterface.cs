using System;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IAimingInterface
    {
        public Vector2 Input { get; set; }
        public event Action OnStateChanged;
        public AimingInterfaceState CurrentState { get; }
        public bool SetState(AimingInterfaceState state);
    }
}