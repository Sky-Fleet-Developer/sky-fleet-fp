using System;
using SphereWorld.Environment.Wind;
using UnityEngine;

namespace SphereWorld
{
    public class Anchor
    {
        public Polar Polar { get; set; }
        public Vector3 GlobalVelocity { get; set; }
        public AnchorParticle ParticlePresentation { get; set; }
    }
}