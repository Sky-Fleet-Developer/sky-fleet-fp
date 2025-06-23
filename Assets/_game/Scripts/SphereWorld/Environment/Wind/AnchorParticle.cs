using System;
using UnityEngine.Serialization;

namespace SphereWorld.Environment.Wind
{
    [Serializable]
    public class AnchorParticle
    {
        public int index;
        public float balloonVolume = 1f;
        public double balloonPressure = 1f;
        public float balloonMass = 1;
        public float verticalDrag = 0.05f;
        public double verticalVelocity = 0;
        public double height = 0;
    }
}