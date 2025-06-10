using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SphereWorld.Environment.Wind
{
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 44)]
    public struct Particle
    {
        [FieldOffset(0)] public float px;
        [FieldOffset(4)] public float py;
        [FieldOffset(8)] public float pz;
        [FieldOffset(12)] public float vx;
        [FieldOffset(16)] public float vy;
        [FieldOffset(20)] public float vz;
        [FieldOffset(24)] public float xGradient;
        [FieldOffset(28)] public float yGradient;
        [FieldOffset(32)] public float zGradient;
        [FieldOffset(36)] public int gridIndex;
        [FieldOffset(40)] public float density;

        public Vector3 GetPosition()
        {
            return new Vector3(px, py, pz);
        }

        public void Randomize()
        {
            Vector3 position = Random.onUnitSphere;
            Vector3 velocity = Vector3.zero;
            px = position.x;
            py = position.y;
            pz = position.z;
            vx = velocity.x;
            vy = velocity.y;
            vz = velocity.z;
            xGradient = 0;
            yGradient = 0;
            zGradient = 0;
            gridIndex = -1;
            density = 0;
        }
    }
}