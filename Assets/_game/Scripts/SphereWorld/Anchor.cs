using System;
using UnityEngine;

namespace SphereWorld
{
    public class Anchor
    {
        public Polar Polar { get; set; }
        public Vector3 GlobalVelocity { get; set; }
        public int ParticlePresentationIndex { get; set; }
    }
}