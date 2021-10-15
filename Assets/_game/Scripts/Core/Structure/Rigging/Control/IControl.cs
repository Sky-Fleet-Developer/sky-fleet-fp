using Core.Structure.Rigging.Control.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    public interface IVisibleControlElement
    {
        public DeviceBase Device { get; set; }
    }
}