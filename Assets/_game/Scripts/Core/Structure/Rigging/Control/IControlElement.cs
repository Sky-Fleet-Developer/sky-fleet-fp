using Core.Structure.Rigging.Control.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{

    public interface IControlElement
    {
        public Port PortAbstact { get; }

        public void Tick();
    }

    public interface IVisibleControlElement : IControlElement
    {
        public DeviceBase Device { get; set; }
    }
}