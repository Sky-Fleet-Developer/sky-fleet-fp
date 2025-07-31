
using UnityEngine;

namespace Runtime.Physic
{
    public static class PhysicsUtilities
    {
        public static Vector3 GetAngularAcceleration(Vector3 size, float mass, Vector3 angleMoment)
        {
            Vector3 inertiaTensor = new Vector3(
                12 / (mass * (size.y*size.y+size.z*size.z)),  
                12 / (mass * (size.x*size.x+size.z*size.z)), 
                12 / (mass * (size.x*size.x+size.y*size.y)));
            
            Vector3 transformated = new Vector3(
                angleMoment.x * inertiaTensor.x, 
                angleMoment.y * inertiaTensor.y, 
                angleMoment.z * inertiaTensor.z);
            return transformated;
        }
    }
}
