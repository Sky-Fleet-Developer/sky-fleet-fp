using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class ThrustInverse : Block, IUpdatableBlock
    {
        public Port<float> portA = new Port<float>(PortType.Thrust);
        public Port<float> portB = new Port<float>(PortType.Thrust);

        [SerializeField] private float trim = 0;

        public void UpdateBlock(int lod)
        {
            float delta = (portA.Value + portB.Value) * 0.5f + trim;
            portA.Value -= delta;
            portB.Value -= delta;
        }
    }
}