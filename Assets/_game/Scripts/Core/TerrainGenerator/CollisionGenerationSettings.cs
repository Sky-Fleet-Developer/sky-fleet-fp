using UnityEngine;

namespace Core.TerrainGenerator
{
    [CreateAssetMenu(menuName = "SF/CollisionGenerationSettings", fileName = "CollisionGenerationSettings")]
    public class CollisionGenerationSettings : ScriptableObject
    {
        public float refreshThreshold = 100;
        public float range = 500;
        public float chunkSize = 100;
        public PhysicsMaterial physicsMaterial;
        public int layer;
    }
}