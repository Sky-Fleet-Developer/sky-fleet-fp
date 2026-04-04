using System.Collections.Generic;
using UnityEngine;

namespace Core.Ai
{
    public class Sensor : ITargetData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 LocalVelocity;
        public float Height;
        public List<SignatureDataWarp> NeighbourSignatures = new();
        public Vector3 MainCaliberWantedDirectionLocalSpace;
        public float MainCaliberChargeInitialSpeed;
        public List<int> MyMenaceTargets = new ();

        Vector3 ITargetData.Position => Position;
        Quaternion ITargetData.Rotation => Rotation;
        Vector3 ITargetData.Velocity => Velocity;
    }
}