using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SphereWorld.Environment.Wind
{
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 48)]
    [Serializable]
    public struct Particle
    {
        [FieldOffset(0)] public Vector3 position;
        [FieldOffset(12)] public Vector3 velocity;
        [FieldOffset(12)] public Vector3 gradient;
        [FieldOffset(36)] public int gridIndex;
        [FieldOffset(40)] public float density;
        [FieldOffset(44)] public float energy;

        public void Randomize()
        {
            Vector3 random = Random.insideUnitSphere * 0.04f;
            //random.z = 0;
            Vector3 p = Random.onUnitSphere * 1.03f + random;
            Vector3 v = Vector3.Cross(p, Vector3.up) * 5f * Mathf.Sign(p.y * Mathf.PI * 0.5f);
            //Vector3 position = Vector3.forward + random;
            //Vector3 velocity = Vector3.forward * 0.1f;
            position.x = p.x;
            position.y = p.y;
            position.z = p.z;
            velocity.x = v.x;
            velocity.y = v.y;
            velocity.z = v.z;
            gradient.x = 0;
            gradient.y = 0;
            gradient.z = 0;
            gridIndex = -1;
            density = 0;
            energy = 5;
        }

        public void SetPosition(Vector3 p)
        {
            position.x = p.x;
            position.y = p.y;
            position.z = p.z;
        }
        public void SetVelocity(Vector3 v)
        {
            velocity.x = v.x;
            velocity.y = v.y;
            velocity.z = v.z;
        }
    }
}